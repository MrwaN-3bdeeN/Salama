using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;
using Salama.Models.DTOs;
using System.Security.Claims;

namespace Salama.Controllers
{
    [Route("api/doctor")]
    [ApiController]
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DoctorDashboardController(AppDbContext context) => _context = context;

        private int GetUserId() => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        // ─── 32. GET OWN PROFILE ───────────────────────────────────
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();

            var doctor = await _context.Doctors
                .Where(d => d.Id == userId)
                .Select(d => new
                {
                    d.Id,
                    Name = d.IdNavigation.Name,
                    Email = d.IdNavigation.Email,
                    Phone = d.IdNavigation.Phone,
                    Address = d.IdNavigation.Address,
                    ProfilePicturePath = d.IdNavigation.ProfilePicturePath,
                    d.About,
                    d.Experience,
                    d.SpecializationId,
                    SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : null
                })
                .FirstOrDefaultAsync();

            if (doctor == null) return NotFound(new { message = "Doctor profile not found." });

            var clinics = await _context.DoctorClinics
                .Where(dc => dc.DoctorId == userId)
                .Select(dc => new
                {
                    Id = dc.Clinic != null ? dc.Clinic.Id : 0,
                    ClinicName = dc.Clinic != null ? dc.Clinic.ClinicName : null,
                    Address = dc.Clinic != null ? dc.Clinic.Address : null,
                    Phone = dc.Clinic != null ? dc.Clinic.Phone : null
                })
                .ToListAsync();

            var certificates = await _context.DoctorCertificates
                .Where(dc => dc.DoctorId == userId)
                .Select(dc => new
                {
                    Id = dc.Certificate != null ? dc.Certificate.Id : 0,
                    CertificateName = dc.Certificate != null ? dc.Certificate.CertificateName : null
                })
                .ToListAsync();

            return Ok(new
            {
                doctor.Id,
                doctor.Name,
                doctor.Email,
                doctor.Phone,
                doctor.Address,
                doctor.ProfilePicturePath,
                doctor.About,
                doctor.Experience,
                doctor.SpecializationId,
                doctor.SpecializationName,
                Clinics = clinics,
                Certificates = certificates
            });
        }

        // ─── 33. UPDATE OWN PROFILE ────────────────────────────────
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileRequest request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            var doctor = await _context.Doctors.FindAsync(userId);
            if (doctor == null) return NotFound(new { message = "Doctor profile not found." });

            if (request.Name != null) user.Name = request.Name;
            if (request.Phone.HasValue) user.Phone = request.Phone.Value;
            if (request.Address != null) user.Address = request.Address;
            if (request.About != null) doctor.About = request.About;
            if (request.Experience != null) doctor.Experience = request.Experience;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully." });
        }

        // ─── 34. UPCOMING APPOINTMENTS ─────────────────────────────
        [HttpGet("appointments/upcoming")]
        public async Task<IActionResult> GetUpcomingAppointments()
        {
            var userId = GetUserId();
            var today = DateOnly.FromDateTime(DateTime.Now);

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == userId && a.AppintmentDate >= today && a.AppointmentStatus != "Cancelled")
                .OrderBy(a => a.AppintmentDate)
                .ThenBy(a => a.AppointmentOrder)
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    PatientName = a.Patient.IdNavigation.Name,
                    PatientId = a.PatientId,
                    Date = a.AppintmentDate,
                    a.AppointmentOrder,
                    a.AppointmentStatus,
                    ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                    ClinicId = a.ClinicId
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // ─── 35. COMPLETED APPOINTMENTS ────────────────────────────
        [HttpGet("appointments/completed")]
        public async Task<IActionResult> GetCompletedAppointments()
        {
            var userId = GetUserId();
            var today = DateOnly.FromDateTime(DateTime.Now);

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == userId && (a.AppintmentDate < today || a.AppointmentStatus == "Completed"))
                .OrderByDescending(a => a.AppintmentDate)
                .ThenByDescending(a => a.AppointmentOrder)
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    PatientName = a.Patient.IdNavigation.Name,
                    PatientId = a.PatientId,
                    Date = a.AppintmentDate,
                    a.AppointmentOrder,
                    a.AppointmentStatus,
                    ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                    ClinicId = a.ClinicId,
                    Diagnosis = a.Diagnoses.Select(d => d.Diagnosis1).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // ─── 36. MARK APPOINTMENT COMPLETED ────────────────────────
        [HttpPut("appointments/{id}/complete")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var userId = GetUserId();
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            if (appointment.DoctorId != userId)
                return Forbid();

            if (appointment.AppointmentStatus == "Completed")
                return BadRequest(new { message = "Appointment is already completed." });

            if (appointment.AppointmentStatus == "Cancelled")
                return BadRequest(new { message = "Cannot complete a cancelled appointment." });

            appointment.AppointmentStatus = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment marked as completed." });
        }

        // ─── 37. CANCEL APPOINTMENT ────────────────────────────────
        [HttpPut("appointments/{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = GetUserId();
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            if (appointment.DoctorId != userId)
                return Forbid();

            if (appointment.AppointmentStatus == "Completed")
                return BadRequest(new { message = "Cannot cancel a completed appointment." });

            if (appointment.AppointmentStatus == "Cancelled")
                return BadRequest(new { message = "Appointment is already cancelled." });

            appointment.AppointmentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled." });
        }

        // ─── 38. LIST OWN PATIENTS ─────────────────────────────────
        [HttpGet("patients")]
        public async Task<IActionResult> GetMyPatients()
        {
            var userId = GetUserId();

            var patients = await _context.Appointments
                .Where(a => a.DoctorId == userId)
                .GroupBy(a => a.PatientId)
                .Select(g => new
                {
                    Id = g.Key,
                    Name = g.First().Patient.IdNavigation.Name,
                    Email = g.First().Patient.IdNavigation.Email,
                    Phone = g.First().Patient.IdNavigation.Phone,
                    LastVisitDate = g.Max(a => a.AppintmentDate)
                })
                .ToListAsync();

            return Ok(patients);
        }

        // ─── 39. VIEW SPECIFIC PATIENT HISTORY ─────────────────────
        [HttpGet("patients/{patientId}/history")]
        public async Task<IActionResult> GetPatientHistory(int patientId)
        {
            var userId = GetUserId();

            var history = await _context.Appointments
                .Where(a => a.DoctorId == userId && a.PatientId == patientId)
                .OrderByDescending(a => a.AppintmentDate)
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    Date = a.AppintmentDate,
                    a.AppointmentStatus,
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

        // ─── 40. CREATE DIAGNOSIS ──────────────────────────────────
        [HttpPost("diagnoses")]
        public async Task<IActionResult> CreateDiagnosis([FromBody] CreateDiagnosisRequest request)
        {
            var userId = GetUserId();

            var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found." });

            if (appointment.DoctorId != userId)
                return Forbid();

            if (appointment.PatientId != request.PatientId)
                return BadRequest(new { message = "Patient ID does not match the appointment." });

            // Check if diagnosis already exists for this appointment
            var exists = await _context.Diagnoses.AnyAsync(d => d.AppointmentId == request.AppointmentId);
            if (exists)
                return BadRequest(new { message = "A diagnosis already exists for this appointment." });

            var maxId = _context.Diagnoses.Any() ? _context.Diagnoses.Max(d => d.Id) : 0;
            var diagnosis = new Diagnosis
            {
                Id = maxId + 1,
                AppointmentId = request.AppointmentId,
                PatientId = request.PatientId,
                DoctorId = userId,
                Diagnosis1 = request.Diagnosis,
                DiagnosisDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.Diagnoses.Add(diagnosis);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Diagnosis created.", id = diagnosis.Id, diagnosis = diagnosis.Diagnosis1, diagnosisDate = diagnosis.DiagnosisDate });
        }

        // ─── 41. UPDATE DIAGNOSIS ──────────────────────────────────
        [HttpPut("diagnoses/{id}")]
        public async Task<IActionResult> UpdateDiagnosis(int id, [FromBody] UpdateDiagnosisRequest request)
        {
            var userId = GetUserId();
            var diagnosis = await _context.Diagnoses.FindAsync(id);

            if (diagnosis == null)
                return NotFound(new { message = "Diagnosis not found." });

            if (diagnosis.DoctorId != userId)
                return Forbid();

            diagnosis.Diagnosis1 = request.Diagnosis;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Diagnosis updated." });
        }

        // ─── 42. LIST OWN CERTIFICATES ─────────────────────────────
        [HttpGet("certificates")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var userId = GetUserId();

            var certs = await _context.DoctorCertificates
                .Where(dc => dc.DoctorId == userId)
                .Select(dc => new
                {
                    Id = dc.Certificate != null ? dc.Certificate.Id : 0,
                    CertificateName = dc.Certificate != null ? dc.Certificate.CertificateName : null
                })
                .ToListAsync();

            return Ok(certs);
        }

        // ─── 43. LIST OWN CLINICS ──────────────────────────────────
        [HttpGet("clinics")]
        public async Task<IActionResult> GetMyClinics()
        {
            var userId = GetUserId();

            var clinics = await _context.DoctorClinics
                .Where(dc => dc.DoctorId == userId)
                .Select(dc => new
                {
                    Id = dc.Clinic != null ? dc.Clinic.Id : 0,
                    ClinicName = dc.Clinic != null ? dc.Clinic.ClinicName : null,
                    Address = dc.Clinic != null ? dc.Clinic.Address : null,
                    Phone = dc.Clinic != null ? dc.Clinic.Phone : null
                })
                .ToListAsync();

            return Ok(clinics);
        }
    }
}
