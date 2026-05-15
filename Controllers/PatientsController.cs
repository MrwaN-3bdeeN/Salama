using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[patient]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("{id}")]
        public IActionResult GetPatientHistory(int id)
        {
            try
            {
                var patient =
                from p in _context.Patients
                join u in _context.Users on p.Id equals u.Id
                join a in _context.Appointments on p.Id equals a.PatientId
                join d in _context.Diagnoses on p.Id equals d.PatientId
                join doc in _context.Doctors on a.DoctorId equals doc.Id
                where p.Id == id
                select new
                    {
                        p.Id,
                        PatientName = p.IdNavigation.Name,
                        Appointments = p.Appointments.Select(a => new
                        {
                            a.Id,
                            a.AppintmentDate,
                            a.AppointmentOrder,
                            a.AppointmentStatus,
                            DoctorName = a.Doctor.IdNavigation.Name
                        }),
                        Diagnoses = p.Diagnoses.Select(d => new
                        {
                            d.Id,
                            d.Diagnosis1,
                            d.DiagnosisDate,
                            DoctorName = d.Doctor.IdNavigation.Name
                        })
                    };
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found.");
                }
                return Ok(patient);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}