using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;
using Salama.Models.DTOs;
using System.Security.Claims;

namespace Salama.Controllers
{
    [Route("api/patient")]
    [ApiController]
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PatientDashboardController(AppDbContext context) => _context = context;

        private int GetUserId() => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        // ─── 44. GET OWN PROFILE ───────────────────────────────────
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();

            var profile = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.Address,
                    u.ProfilePicturePath
                })
                .FirstOrDefaultAsync();

            if (profile == null) return NotFound(new { message = "Patient not found." });
            return Ok(profile);
        }

        // ─── 45. UPDATE OWN PROFILE ────────────────────────────────
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileRequest request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound(new { message = "Patient not found." });

            if (request.Name != null) user.Name = request.Name;
            if (request.Phone.HasValue) user.Phone = request.Phone.Value;
            if (request.Address != null) user.Address = request.Address;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully." });
        }

        // ─── 46. LIST OWN APPOINTMENTS ─────────────────────────────
        [HttpGet("appointments")]
        public async Task<IActionResult> GetMyAppointments([FromQuery] string? status)
        {
            var userId = GetUserId();

            var query = _context.Appointments
                .Where(a => a.PatientId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.AppointmentStatus == status);

            var appointments = await query
                .OrderByDescending(a => a.AppintmentDate)
                .ThenByDescending(a => a.AppointmentOrder)
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    Date = a.AppintmentDate,
                    a.AppointmentOrder,
                    a.AppointmentStatus,
                    DoctorName = a.Doctor.IdNavigation.Name,
                    DoctorId = a.DoctorId,
                    ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                    ClinicId = a.ClinicId,
                    Diagnosis = a.Diagnoses.Select(d => d.Diagnosis1).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // ─── 47. CANCEL OWN APPOINTMENT ────────────────────────────
        [HttpPut("appointments/{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = GetUserId();
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            if (appointment.PatientId != userId)
                return Forbid();

            if (appointment.AppointmentStatus == "Completed")
                return BadRequest(new { message = "Cannot cancel a completed appointment." });

            if (appointment.AppointmentStatus == "Cancelled")
                return BadRequest(new { message = "Appointment is already cancelled." });

            DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
            var remainingTime = appointmentDateTime - DateTime.Now;

            if (remainingTime.TotalHours < 72)
                return BadRequest(new { message = "Cannot cancel appointments within 72 hours of the scheduled time." });

            if (appointment.AppintmentDate < DateOnly.FromDateTime(DateTime.Now))
                return BadRequest(new { message = "Cannot cancel past appointments." });

            appointment.AppointmentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled successfully." });
        }

        // ─── 47b. UPDATE OWN APPOINTMENT DATE ───────────────────────
        [HttpPut("appointments/{id}/date")]
        public async Task<IActionResult> UpdateAppointmentDate(int id, [FromBody] UpdatePatientAppointmentDateRequest request)
        {
            var userId = GetUserId();
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            if (appointment.PatientId != userId)
                return Forbid();

            if (appointment.AppointmentStatus == "Completed")
                return BadRequest(new { message = "Cannot update a completed appointment." });

            if (appointment.AppointmentStatus == "Cancelled")
                return BadRequest(new { message = "Cannot update a cancelled appointment." });

            DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
            var remainingTime = appointmentDateTime - DateTime.Now;

            if (remainingTime.TotalHours < 72)
                return BadRequest(new { message = "Cannot update appointments within 72 hours of the scheduled time." });

            if (appointment.AppintmentDate < DateOnly.FromDateTime(DateTime.Now))
                return BadRequest(new { message = "Cannot update past appointments." });

            appointment.AppintmentDate = request.NewDate;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment updated successfully." });
        }

        // ─── 48. LIST OWN DIAGNOSES ────────────────────────────────
        [HttpGet("diagnoses")]
        public async Task<IActionResult> GetMyDiagnoses()
        {
            var userId = GetUserId();

            var diagnoses = await _context.Diagnoses
                .Where(d => d.PatientId == userId)
                .OrderByDescending(d => d.DiagnosisDate)
                .Select(d => new
                {
                    d.Id,
                    d.Diagnosis1,
                    d.DiagnosisDate,
                    DoctorName = d.Doctor.IdNavigation.Name,
                    d.AppointmentId
                })
                .ToListAsync();

            return Ok(diagnoses);
        }

        // ─── 49. FULL MEDICAL HISTORY ──────────────────────────────
        [HttpGet("history")]
        public async Task<IActionResult> GetMedicalHistory()
        {
            var userId = GetUserId();

            var history = await _context.Appointments
                .Where(a => a.PatientId == userId)
                .OrderByDescending(a => a.AppintmentDate)
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    Date = a.AppintmentDate,
                    a.AppointmentStatus,
                    DoctorName = a.Doctor.IdNavigation.Name,
                    DoctorId = a.DoctorId,
                    Specialization = a.Doctor.Specialization != null ? a.Doctor.Specialization.SpecializationName : null,
                    ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                    Diagnosis = a.Diagnoses.Select(d => new
                    {
                        d.Id,
                        d.Diagnosis1,
                        d.DiagnosisDate
                    }).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(history);
        }
    }

    public class UpdatePatientAppointmentDateRequest
    {
        public DateOnly NewDate { get; set; }
    }
}
