using System.ComponentModel.DataAnnotations;

namespace Salama.Models.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [Range(100000000, 2147483647)]
        public int Phone { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        [RegularExpression("^(Admin|Doctor|Patient)$")]
        public string Role { get; set; } = null!;

        public string? Address { get; set; }

        // Doctor-specific (required if Role == "Doctor")
        public string? About { get; set; }
        public string? Experience { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }

    public class VerifyPasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = null!;
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiry { get; set; }
        public UserResponse User { get; set; } = null!;
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Phone { get; set; }
        public string Role { get; set; } = null!;
        public string? Address { get; set; }
        public string? ProfilePicturePath { get; set; }
    }
}
