using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;
using Salama.Models.DTOs;
using BCrypt.Net;

namespace Salama.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminController(AppDbContext context) => _context = context;

        private static int GetNextId<T>(DbSet<T> set) where T : class
        {
            var maxId = set.Any() ? set.Max(e => EF.Property<int>(e, "Id")) : 0;
            return maxId + 1;
        }

        private const string MsgDoctorNotFound = "Doctor not found.";
        private const string MsgDoctorProfileNotFound = "Doctor profile not found.";
        private const string MsgPatientNotFound = "Patient not found.";
        private const string MsgClinicNotFound = "Clinic not found.";
        private const string MsgSpecializationNotFound = "Specialization not found.";
        private const string MsgCertificateNotFound = "Certificate not found.";
        private const string MsgAssignmentNotFound = "Assignment not found.";

        // ─── 7. DASHBOARD STATS ────────────────────────────────────
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var totalDoctors = await _context.Doctors.CountAsync();
            var totalPatients = await _context.Patients.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync();
            var upcomingAppointments = await _context.Appointments.CountAsync(a => a.AppintmentDate >= today);
            var completedAppointments = await _context.Appointments.CountAsync(a => a.AppintmentDate < today);
            var totalClinics = await _context.Clinics.CountAsync();
            var totalSpecializations = await _context.Specializations.CountAsync();

            return Ok(new
            {
                totalDoctors,
                totalPatients,
                totalAppointments,
                upcomingAppointments,
                completedAppointments,
                totalClinics,
                totalSpecializations
            });
        }

        // ─── DOCTORS CRUD ──────────────────────────────────────────

        // 8. List all doctors
        [HttpGet("doctors")]
        public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _context.Doctors
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
                .ToListAsync();

            return Ok(doctors);
        }

        // 9. Create doctor
        [HttpPost("doctors")]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already exists." });

            var userId = GetNextId(_context.Users);
            var user = new User
            {
                Id = userId,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = "Doctor",
                Address = request.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };
            _context.Users.Add(user);

            var doctor = new Doctor
            {
                Id = userId,
                About = request.About ?? "",
                Experience = request.Experience ?? "",
                SpecializationId = request.SpecializationId
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Doctor created successfully.", id = userId, name = user.Name, email = user.Email, role = user.Role });
        }

        // 10. Update doctor
        [HttpPut("doctors/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Doctor")
                return NotFound(new { message = MsgDoctorNotFound });

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound(new { message = MsgDoctorProfileNotFound });

            if (request.Name != null) user.Name = request.Name;
            if (request.Email != null) user.Email = request.Email;
            if (request.Phone.HasValue) user.Phone = request.Phone.Value;
            if (request.Address != null) user.Address = request.Address;
            if (request.About != null) doctor.About = request.About;
            if (request.Experience != null) doctor.Experience = request.Experience;
            if (request.SpecializationId.HasValue) doctor.SpecializationId = request.SpecializationId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Doctor updated successfully." });
        }

        // 11. Delete doctor
        [HttpDelete("doctors/{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Doctor")
                return NotFound(new { message = MsgDoctorNotFound });

            // Remove related records first
            var doctorClinics = await _context.DoctorClinics.Where(dc => dc.DoctorId == id).ToListAsync();
            _context.DoctorClinics.RemoveRange(doctorClinics);

            var doctorCertificates = await _context.DoctorCertificates.Where(dc => dc.DoctorId == id).ToListAsync();
            _context.DoctorCertificates.RemoveRange(doctorCertificates);

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null) _context.Doctors.Remove(doctor);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Doctor deleted successfully." });
        }

        // ─── PATIENTS CRUD ─────────────────────────────────────────

        // 12. List all patients
        [HttpGet("patients")]
        public async Task<IActionResult> GetAllPatients()
        {
            var patients = await _context.Patients
                .Select(p => new
                {
                    p.Id,
                    Name = p.IdNavigation.Name,
                    Email = p.IdNavigation.Email,
                    Phone = p.IdNavigation.Phone,
                    Address = p.IdNavigation.Address,
                    ProfilePicturePath = p.IdNavigation.ProfilePicturePath
                })
                .ToListAsync();

            return Ok(patients);
        }

        // 13. Create patient
        [HttpPost("patients")]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already exists." });

            var userId = GetNextId(_context.Users);
            var user = new User
            {
                Id = userId,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = "Patient",
                Address = request.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };
            _context.Users.Add(user);

            var patient = new Patient { Id = userId };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Patient created successfully.", id = userId, name = user.Name, email = user.Email, role = user.Role });
        }

        // 14. Update patient
        [HttpPut("patients/{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Patient")
                return NotFound(new { message = MsgPatientNotFound });

            if (request.Name != null) user.Name = request.Name;
            if (request.Email != null) user.Email = request.Email;
            if (request.Phone.HasValue) user.Phone = request.Phone.Value;
            if (request.Address != null) user.Address = request.Address;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Patient updated successfully." });
        }

        // 15. Delete patient
        [HttpDelete("patients/{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Patient")
                return NotFound(new { message = MsgPatientNotFound });

            var patient = await _context.Patients.FindAsync(id);
            if (patient != null) _context.Patients.Remove(patient);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Patient deleted successfully." });
        }

        // ─── 16. ALL APPOINTMENTS ──────────────────────────────────
        [HttpGet("appointments")]
        public async Task<IActionResult> GetAllAppointments(
            [FromQuery] string? status,
            [FromQuery] int? doctorId,
            [FromQuery] string? date)
        {
            var query = _context.Appointments.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.AppointmentStatus == status);

            if (doctorId.HasValue)
                query = query.Where(a => a.DoctorId == doctorId.Value);

            if (DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var filterDateTime))
            {
                var filterDate = DateOnly.FromDateTime(filterDateTime);
                query = query.Where(a => a.AppintmentDate == filterDate);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppintmentDate)
                .ThenByDescending(a => a.AppointmentOrder)
                .Select(a => new
                {
                    a.Id,
                    Date = a.AppintmentDate,
                    a.AppointmentOrder,
                    a.AppointmentStatus,
                    PatientName = a.Patient.IdNavigation.Name,
                    PatientId = a.PatientId,
                    DoctorName = a.Doctor.IdNavigation.Name,
                    DoctorId = a.DoctorId,
                    ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                    ClinicId = a.ClinicId
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // ─── CLINICS CRUD ──────────────────────────────────────────

        // 17. List all clinics
        [HttpGet("clinics")]
        public async Task<IActionResult> GetAllClinics()
        {
            var clinics = await _context.Clinics
                .Select(c => new
                {
                    c.Id,
                    c.ClinicName,
                    c.Address,
                    c.Phone,
                    c.SpecializationId,
                    SpecializationName = c.Specialization != null ? c.Specialization.SpecializationName : null
                })
                .ToListAsync();

            return Ok(clinics);
        }

        // 18. Create clinic
        [HttpPost("clinics")]
        public async Task<IActionResult> CreateClinic([FromBody] CreateClinicRequest request)
        {
            var clinic = new Clinic
            {
                Id = GetNextId(_context.Clinics),
                ClinicName = request.ClinicName,
                Address = request.Address,
                Phone = request.Phone,
                SpecializationId = request.SpecializationId
            };

            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Clinic created successfully.", id = clinic.Id, clinicName = clinic.ClinicName });
        }

        // 19. Update clinic
        [HttpPut("clinics/{id}")]
        public async Task<IActionResult> UpdateClinic(int id, [FromBody] UpdateClinicRequest request)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
                return NotFound(new { message = MsgClinicNotFound });

            if (request.ClinicName != null) clinic.ClinicName = request.ClinicName;
            if (request.Address != null) clinic.Address = request.Address;
            if (request.Phone != null) clinic.Phone = request.Phone;
            if (request.SpecializationId.HasValue) clinic.SpecializationId = request.SpecializationId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Clinic updated successfully." });
        }

        // 20. Delete clinic
        [HttpDelete("clinics/{id}")]
        public async Task<IActionResult> DeleteClinic(int id)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
                return NotFound(new { message = MsgClinicNotFound });

            var doctorClinics = await _context.DoctorClinics.Where(dc => dc.ClinicId == id).ToListAsync();
            _context.DoctorClinics.RemoveRange(doctorClinics);

            _context.Clinics.Remove(clinic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Clinic deleted successfully." });
        }

        // ─── SPECIALIZATIONS CRUD ──────────────────────────────────

        // 21. List all specializations
        [HttpGet("specializations")]
        public async Task<IActionResult> GetAllSpecializations()
        {
            var specs = await _context.Specializations
                .Select(s => new { s.Id, s.SpecializationName })
                .ToListAsync();

            return Ok(specs);
        }

        // 22. Create specialization
        [HttpPost("specializations")]
        public async Task<IActionResult> CreateSpecialization([FromBody] CreateSpecializationRequest request)
        {
            var spec = new Specialization
            {
                Id = GetNextId(_context.Specializations),
                SpecializationName = request.SpecializationName
            };

            _context.Specializations.Add(spec);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specialization created successfully.", id = spec.Id, specializationName = spec.SpecializationName });
        }

        // 23. Update specialization
        [HttpPut("specializations/{id}")]
        public async Task<IActionResult> UpdateSpecialization(int id, [FromBody] UpdateSpecializationRequest request)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec == null)
                return NotFound(new { message = MsgSpecializationNotFound });

            if (request.SpecializationName != null) spec.SpecializationName = request.SpecializationName;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Specialization updated successfully." });
        }

        // 24. Delete specialization
        [HttpDelete("specializations/{id}")]
        public async Task<IActionResult> DeleteSpecialization(int id)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec == null)
                return NotFound(new { message = MsgSpecializationNotFound });

            _context.Specializations.Remove(spec);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specialization deleted successfully." });
        }

        // ─── CERTIFICATES CRUD ─────────────────────────────────────

        // 25. List all certificates
        [HttpGet("certificates")]
        public async Task<IActionResult> GetAllCertificates()
        {
            var certs = await _context.Certificates
                .Select(c => new { c.Id, c.CertificateName })
                .ToListAsync();

            return Ok(certs);
        }

        // 26. Create certificate
        [HttpPost("certificates")]
        public async Task<IActionResult> CreateCertificate([FromBody] CreateCertificateRequest request)
        {
            var cert = new Certificate
            {
                Id = GetNextId(_context.Certificates),
                CertificateName = request.CertificateName
            };

            _context.Certificates.Add(cert);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Certificate created successfully.", id = cert.Id, certificateName = cert.CertificateName });
        }

        // 27. Delete certificate
        [HttpDelete("certificates/{id}")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var cert = await _context.Certificates.FindAsync(id);
            if (cert == null)
                return NotFound(new { message = MsgCertificateNotFound });

            var doctorCerts = await _context.DoctorCertificates.Where(dc => dc.CertificateId == id).ToListAsync();
            _context.DoctorCertificates.RemoveRange(doctorCerts);

            _context.Certificates.Remove(cert);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Certificate deleted successfully." });
        }

        // ─── DOCTOR-CLINIC ASSIGNMENTS ─────────────────────────────

        // 28. Assign doctor to clinic
        [HttpPost("doctors/{id}/clinics")]
        public async Task<IActionResult> AssignDoctorToClinic(int id, [FromBody] AssignDoctorClinicRequest request)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound(new { message = MsgDoctorNotFound });

            var clinic = await _context.Clinics.FindAsync(request.ClinicId);
            if (clinic == null) return NotFound(new { message = MsgClinicNotFound });

            var exists = await _context.DoctorClinics.AnyAsync(dc => dc.DoctorId == id && dc.ClinicId == request.ClinicId);
            if (exists) return BadRequest(new { message = "Doctor is already assigned to this clinic." });

            _context.DoctorClinics.Add(new DoctorClinic { DoctorId = id, ClinicId = request.ClinicId });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Doctor assigned to clinic successfully." });
        }

        // 29. Remove doctor from clinic
        [HttpDelete("doctors/{id}/clinics/{clinicId}")]
        public async Task<IActionResult> RemoveDoctorFromClinic(int id, int clinicId)
        {
            var dc = await _context.DoctorClinics.FirstOrDefaultAsync(dc => dc.DoctorId == id && dc.ClinicId == clinicId);
            if (dc == null) return NotFound(new { message = MsgAssignmentNotFound });

            _context.DoctorClinics.Remove(dc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Doctor removed from clinic successfully." });
        }

        // ─── DOCTOR-CERTIFICATE ASSIGNMENTS ────────────────────────

        // 30. Assign certificate to doctor
        [HttpPost("doctors/{id}/certificates")]
        public async Task<IActionResult> AssignCertificateToDoctor(int id, [FromBody] AssignDoctorCertificateRequest request)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound(new { message = MsgDoctorNotFound });

            var cert = await _context.Certificates.FindAsync(request.CertificateId);
            if (cert == null) return NotFound(new { message = MsgCertificateNotFound });

            var exists = await _context.DoctorCertificates.AnyAsync(dc => dc.DoctorId == id && dc.CertificateId == request.CertificateId);
            if (exists) return BadRequest(new { message = "Doctor already has this certificate." });

            _context.DoctorCertificates.Add(new DoctorCertificate { DoctorId = id, CertificateId = request.CertificateId });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Certificate assigned to doctor successfully." });
        }

        // 31. Remove certificate from doctor
        [HttpDelete("doctors/{id}/certificates/{certId}")]
        public async Task<IActionResult> RemoveCertificateFromDoctor(int id, int certId)
        {
            var dc = await _context.DoctorCertificates.FirstOrDefaultAsync(dc => dc.DoctorId == id && dc.CertificateId == certId);
            if (dc == null) return NotFound(new { message = MsgAssignmentNotFound });

            _context.DoctorCertificates.Remove(dc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Certificate removed from doctor successfully." });
        }
    }
}
