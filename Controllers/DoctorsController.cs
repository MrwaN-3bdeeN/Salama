using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult GetAllDoctor()
        {
            try
            {
                var doctorClinics = _context.DoctorClinics;
                var doctors = _context.Doctors
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : null,
                        UserName = d.IdNavigation.Name,
                        Address = d.IdNavigation.Address,
                        ClinicName = doctorClinics.Where(dc => dc.DoctorId == d.Id)
                            .Select(dc => dc.Clinic.ClinicName)
                            .FirstOrDefault()
                        })
                    .ToList();
                if (doctors == null || doctors.Count == 0)
                {
                    return NotFound("No doctors == found.");
                }
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpGet("{id}")]
        public IActionResult GetDoctorById(int id)
        {
            try
            {
                var doctor =
                from dc in _context.DoctorClinics
                join d in _context.Doctors
                    on dc.DoctorId equals d.Id
                join c in _context.Clinics
                    on dc.ClinicId equals c.Id
                where d.Id == id
                select new
                {
                    d.Id,
                    d.About,
                    d.Experience,
                    d.SpecializationId,
                    SpecializationName =
                        d.Specialization != null
                       ? d.Specialization.SpecializationName
                        : null,
                    UserName = d.IdNavigation.Name,
                    Address = d.IdNavigation.Address,
                    ClinicName = c.ClinicName
                };

                var result = doctor.FirstOrDefault();

                if (result == null)
                    return NotFound("No doctors found with this id.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{clinicId}/doctors")]
        public IActionResult GetDoctorsByClinic(int clinicId)
        {
            var doctors =
            from dc in _context.DoctorClinics
            join d in _context.Doctors
                on dc.DoctorId equals d.Id
            join c in _context.Clinics
                on dc.ClinicId equals c.Id
            where c.Id == clinicId
            select new
            {
                d.Id,
                d.About,
                d.Experience,
                d.SpecializationId,
                SpecializationName =
                    d.Specialization != null
                    ? d.Specialization.SpecializationName
                    : null,
                UserName = d.IdNavigation.Name,
                Address = d.IdNavigation.Address,
                ClinicName = c.ClinicName
            };

            var result = doctors.ToList();

            if (!result.Any())
                return NotFound("No doctors found for this clinic.");

            return Ok(result);
        }



        
        [HttpGet("{specializationid}/specialization")]
        public IActionResult GetDoctorsBySpecialization(int specializationid)
        {
            try
            {
                var spec =
                from s in _context.Specializations
                join d in _context.Doctors
                    on s.Id equals d.SpecializationId
                where s.Id == specializationid
                select new
                {
                    s.Id,
                    s.SpecializationName,
                    d.About,
                    d.Experience,
                    d.IdNavigation.Name,
                    d.IdNavigation.Phone,
                    d.IdNavigation.Address
                };
                var result = spec.ToList();
                if (result == null || result.Count == 0)
                {
                    return NotFound("No doctors found for this specialization.");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    


        [HttpGet("{id}/clinics")]
        public IActionResult GetClinicsByDoctorId(int id)
        {
            var clinics = _context.DoctorClinics
            .Where(dc => dc.DoctorId == id)
            .Select(dc => new
            {
                dc.Clinic.Id,
                dc.Clinic.ClinicName,
                dc.Clinic.Address,
                dc.Clinic.Phone
            })
            .ToList();

            return Ok(clinics);
        
        }
    }
}