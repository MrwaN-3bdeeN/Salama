using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/doctors")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DoctorsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        public IActionResult GetAllDoctor(
            [FromQuery] int? specializationId,
            [FromQuery] int? clinicId,
            [FromQuery] string? location,
            [FromQuery] string? search,
            [FromQuery] string? clinicName)
        {
            try
            {
                var query = _context.Doctors
                    .Where(d => !specializationId.HasValue || d.SpecializationId == specializationId);

                if (clinicId.HasValue)
                {
                    var doctorIdsInClinic = _context.DoctorClinics
                        .Where(dc => dc.ClinicId == clinicId && dc.DoctorId != null)
                        .Select(dc => dc.DoctorId!.Value)
                        .Distinct();
                    query = query.Where(d => doctorIdsInClinic.Contains(d.Id));
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    var loc = location.Trim().ToLower();
                    query = query.Where(d => d.IdNavigation.Address != null && d.IdNavigation.Address.ToLower().Contains(loc));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(d => d.IdNavigation.Name.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(clinicName))
                {
                    var cn = clinicName.Trim().ToLower();
                    var doctorIdsInClinic = _context.DoctorClinics
                        .Where(dc => dc.Clinic != null && dc.Clinic.ClinicName != null && dc.Clinic.ClinicName.ToLower().Contains(cn) && dc.DoctorId != null)
                        .Select(dc => dc.DoctorId!.Value)
                        .Distinct();
                    query = query.Where(d => doctorIdsInClinic.Contains(d.Id));
                }

                var doctorIds = query.Select(d => d.Id).ToList();

                var doctorClinicMap = _context.DoctorClinics
                    .Where(dc => dc.DoctorId.HasValue && doctorIds.Contains(dc.DoctorId.Value) && dc.Clinic != null)
                    .GroupBy(dc => dc.DoctorId!.Value)
                    .Select(g => new { DoctorId = g.Key, ClinicName = g.First().Clinic!.ClinicName })
                    .ToList()
                    .ToDictionary(x => x.DoctorId, x => x.ClinicName);

                var doctors = _context.Doctors
                    .Where(d => doctorIds.Contains(d.Id))
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : null,
                        UserName = d.IdNavigation!.Name,
                        Address = d.IdNavigation.Address
                    })
                    .ToList()
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        d.SpecializationName,
                        d.UserName,
                        d.Address,
                        ClinicName = doctorClinicMap.ContainsKey(d.Id) ? doctorClinicMap[d.Id] : null
                    })
                    .ToList();

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDoctorById(int id)
        {
            try
            {
                var doctor = _context.Doctors
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : null,
                        UserName = d.IdNavigation!.Name,
                        ProfilePicturePath = d.IdNavigation.ProfilePicturePath,
                        Address = d.IdNavigation.Address,
                        Email = d.IdNavigation.Email
                    })
                    .FirstOrDefault();

                if (doctor == null)
                    return NotFound(new { message = "No doctors found with this id." });

                var clinics = _context.DoctorClinics
                    .Where(dc => dc.DoctorId == id && dc.Clinic != null)
                    .Select(dc => new
                    {
                        dc.Clinic!.Id,
                        dc.Clinic.ClinicName,
                        dc.Clinic.Address,
                        dc.Clinic.Phone
                    })
                    .ToList();

                var certificates = _context.DoctorCertificates
                    .Where(dc => dc.DoctorId == id && dc.Certificate != null)
                    .Select(dc => new
                    {
                        dc.Certificate!.Id,
                        dc.Certificate.CertificateName
                    })
                    .ToList();

                return Ok(new { doctor.Id, doctor.About, doctor.Experience, doctor.SpecializationId, doctor.SpecializationName, doctor.UserName, doctor.ProfilePicturePath, doctor.Address, doctor.Email, Clinics = clinics, Certificates = certificates });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-clinic/{clinicId}")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDoctorsByClinic(int clinicId)
        {
            try
            {
                var doctors = _context.DoctorClinics
                    .Where(dc => dc.ClinicId == clinicId && dc.Doctor != null)
                    .Select(dc => new
                    {
                        dc.Doctor!.Id,
                        dc.Doctor.About,
                        dc.Doctor.Experience,
                        dc.Doctor.SpecializationId,
                        SpecializationName = dc.Doctor.Specialization != null ? dc.Doctor.Specialization.SpecializationName : null,
                        UserName = dc.Doctor!.IdNavigation!.Name,
                        Address = dc.Doctor.IdNavigation.Address,
                        ClinicName = dc.Clinic != null ? dc.Clinic.ClinicName : null
                    })
                    .ToList();

                if (!doctors.Any())
                    return NotFound(new { message = "No doctors found for this clinic." });

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-specialization/{specializationId}")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDoctorsBySpecialization(int specializationId)
        {
            try
            {
                var doctors = _context.Doctors
                    .Where(d => d.SpecializationId == specializationId)
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : null,
                        UserName = d.IdNavigation!.Name,
                        Phone = d.IdNavigation.Phone,
                        Address = d.IdNavigation.Address
                    })
                    .ToList();

                if (!doctors.Any())
                    return NotFound(new { message = "No doctors found for this specialization." });

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/clinics")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        public IActionResult GetClinicsByDoctorId(int id)
        {
            try
            {
                var clinics = _context.DoctorClinics
                    .Where(dc => dc.DoctorId == id && dc.Clinic != null)
                    .Select(dc => new
                    {
                        dc.Clinic!.Id,
                        dc.Clinic.ClinicName,
                        dc.Clinic.Address,
                        dc.Clinic.Phone
                    })
                    .ToList();

                return Ok(clinics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
