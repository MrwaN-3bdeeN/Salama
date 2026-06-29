using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;
using Salama.Models.DTOs;
using BCrypt.Net;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Salama.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace Salama.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JWT _jwt;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, IOptions<JWT> jwtOptions, EmailService emailService)
        {
            _context = context;
            _jwt = jwtOptions.Value;
            _emailService = emailService;
        }

        // ─── 1. REGISTER ───────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already exists." });

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Id = GetNextUserId(),
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                Address = request.Address,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(newUser);

            // Create the sub-record based on role
            switch (request.Role)
            {
                case "Patient":
                    var patient = new Patient { Id = newUser.Id };
                    _context.Patients.Add(patient);
                    break;

                case "Doctor":
                    var doctor = new Doctor
                    {
                        Id = newUser.Id,
                        About = request.About ?? "",
                        Experience = request.Experience ?? "",
                        SpecializationId = request.SpecializationId
                    };
                    _context.Doctors.Add(doctor);
                    break;

                case "Admin":
                    var admin = new Admin { Id = newUser.Id };
                    _context.Admins.Add(admin);
                    break;
            }

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(newUser);
            var refreshToken = GenerateRefreshToken();
            newUser.RefreshToken = refreshToken.Token;
            newUser.RefreshTokenExpiry = refreshToken.Expiry;
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiry = refreshToken.Expiry,
                User = MapToUserResponse(newUser)
            });
        }

        // ─── 2. LOGIN ──────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (dbUser == null)
                return BadRequest(new { message = "Invalid email or password." });

            bool isCorrect = BCrypt.Net.BCrypt.Verify(request.Password, dbUser.PasswordHash);
            if (!isCorrect)
                return BadRequest(new { message = "Invalid password." });

            var token = GenerateJwtToken(dbUser);
            var refreshToken = GenerateRefreshToken();
            dbUser.RefreshToken = refreshToken.Token;
            dbUser.RefreshTokenExpiry = refreshToken.Expiry;
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiry = refreshToken.Expiry,
                User = MapToUserResponse(dbUser)
            });
        }

        // ─── 3. REFRESH TOKEN ──────────────────────────────────────
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == request.RefreshToken);

            if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken.Token;
            user.RefreshTokenExpiry = newRefreshToken.Expiry;
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiry = newRefreshToken.Expiry,
                User = MapToUserResponse(user)
            });
        }

        // ─── 4. GET CURRENT USER ───────────────────────────────────
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(MapToUserResponse(user));
        }

        // ─── 5a. VERIFY PASSWORD ───────────────────────────────────
        [Authorize]
        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword([FromBody] VerifyPasswordRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            bool isCorrect = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            if (!isCorrect)
                return BadRequest(new { message = "Current password is incorrect." });

            return Ok(new { valid = true });
        }

        // ─── 5b. CHANGE PASSWORD ───────────────────────────────────
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            bool isCorrect = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            if (!isCorrect)
                return BadRequest(new { message = "Current password is incorrect." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Invalidate all refresh tokens on password change
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }

        // ─── 5c. SEND EMAIL VERIFICATION CODE ─────────────────────
        [Authorize]
        [HttpPost("send-email-verification")]
        public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.NewEmail);
            if (existingUser != null)
                return BadRequest(new { message = "This email is already in use." });

            var code = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.VerificationCode = code;
            user.VerificationCodeExpiry = expiry;
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendVerificationCodeAsync(request.NewEmail, code);
                return Ok(new { message = "Verification code sent to your new email." });
            }
            catch
            {
                return BadRequest(new { message = "Failed to send verification email. Please try again." });
            }
        }

        // ─── 5d. CHANGE EMAIL WITH VERIFICATION ────────────────────
        [Authorize]
        [HttpPut("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.VerificationCode != request.Code || user.VerificationCodeExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired verification code." });

            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.NewEmail && u.Id != userId);
            if (emailExists)
                return BadRequest(new { message = "This email is already in use." });

            user.Email = request.NewEmail;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;
            await _context.SaveChangesAsync();

            var updatedUser = MapToUserResponse(user);
            return Ok(new { message = "Email changed successfully.", user = updatedUser });
        }

        // ─── 6. FORGOT PASSWORD ────────────────────────────────────
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Always return OK to prevent email enumeration
            if (user == null)
                return Ok(new { message = "If the email exists, a reset link has been sent." });

            var resetToken = GenerateRefreshToken();
            user.RefreshToken = resetToken.Token;
            user.RefreshTokenExpiry = resetToken.Expiry;
            await _context.SaveChangesAsync();

            // TODO: Send email with reset token link
            // For now, return the token in development only
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return Ok(new
                {
                    message = "Reset token generated (dev mode — token returned in response).",
                    resetToken = resetToken.Token,
                    expiresAt = resetToken.Expiry
                });
            }

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        // ─── UPLOAD PROFILE PICTURE ────────────────────────────────
        [Authorize]
        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Only JPG, PNG, and WebP images are allowed." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size must be under 5MB." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"user_{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Delete old picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var oldPath = Path.Combine(uploadsFolder, user.ProfilePicturePath);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            user.ProfilePicturePath = uniqueFileName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture uploaded successfully.", fileName = uniqueFileName });
        }

        // ─── HELPERS ───────────────────────────────────────────────

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private (string Token, DateTime Expiry) GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return (
                Token: Convert.ToBase64String(randomBytes),
                Expiry: DateTime.UtcNow.AddDays((int)_jwt.DurationInDays)
            );
        }

        private int? GetUserIdFromToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out var userId) ? userId : null;
        }

        private int GetNextUserId()
        {
            var maxId = _context.Users.Any() ? _context.Users.Max(u => u.Id) : 0;
            return maxId + 1;
        }

        private static UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Address = user.Address,
                ProfilePicturePath = user.ProfilePicturePath
            };
        }
    }
}
