using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }

        // FIX: Materialize query before null check, fix diagnosis join
        [HttpGet("{id}/history")]
        public IActionResult GetPatientHistory(int id)
        {
            try
            {
                var patientExists = _context.Patients.Any(p => p.Id == id);
                if (!patientExists)
                    return NotFound(new { message = $"Patient with ID {id} not found." });

                var history = _context.Appointments
                    .Where(a => a.PatientId == id)
                    .Select(a => new
                    {
                        AppointmentId = a.Id,
                        PatientName = a.Patient.IdNavigation.Name,
                        DoctorName = a.Doctor.IdNavigation.Name,
                        DoctorId = a.DoctorId,
                        a.AppintmentDate,
                        a.AppointmentOrder,
                        a.AppointmentStatus,
                        ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                        Diagnosis = a.Diagnoses.Select(d => new
                        {
                            d.Id,
                            d.Diagnosis1,
                            d.DiagnosisDate
                        }).FirstOrDefault()
                    })
                    .OrderByDescending(a => a.AppintmentDate)
                    .ToList();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
