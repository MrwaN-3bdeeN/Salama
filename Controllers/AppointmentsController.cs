using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]

    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        public class BookAppointmentRequest
        {
            public int DoctorId { get; set; }
            public int PatientId { get; set; }
            public int ClinicId { get; set; }
            public string Diagnosis { get; set; } = null!;
            public DateOnly AppointmentDate { get; set; }
        }

        [HttpPost]
        public IActionResult BookAppointment([FromBody] BookAppointmentRequest request)
        {
            try
            {
                var newId = (_context.Appointments.Max(a => (int?)a.Id) ?? 0) + 1;
                var order = (_context.Appointments.Where(a => a.DoctorId == request.DoctorId && a.AppintmentDate == request.AppointmentDate).Max(a => (int?)a.AppointmentOrder) ?? 0) + 1;

                var appointment = new Appointment
                {
                    Id = newId,
                    DoctorId = request.DoctorId,
                    PatientId = request.PatientId,
                    AppintmentDate = request.AppointmentDate,
                    AppointmentOrder = order,
                    AppointmentStatus = "Scheduled"
                };

                _context.Appointments.Add(appointment);
                _context.SaveChanges();

                var diagnosisId = (_context.Diagnoses.Max(d => (int?)d.Id) ?? 0) + 1;
                var diagnosisEntry = new Diagnosis
                {
                    Id = diagnosisId,
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    AppointmentId = newId,
                    DiagnosisDate = request.AppointmentDate,
                    Diagnosis1 = request.Diagnosis
                };

                _context.Diagnoses.Add(diagnosisEntry);
                _context.SaveChanges();

                return Ok(new { Message = "Appointment booked successfully.", AppointmentId = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetAppointmentInfo(int id)
        {
            try
            {
                var appointment = _context.Appointments
                    .Where(a => a.Id == id)
                    .Select(a => new
                    {
                        a.Id,
                        a.AppintmentDate,
                        a.AppointmentOrder,
                        a.AppointmentStatus,
                        DoctorName = a.Doctor.IdNavigation.Name,
                        PatientName = a.Patient.IdNavigation.Name,
                        ClinicName = _context.DoctorClinics
                            .Where(dc => dc.DoctorId == a.DoctorId)
                            .Select(dc => dc.Clinic.ClinicName)
                            .FirstOrDefault(),
                        Diagnoses = a.Diagnoses.Select(d => new
                        {
                            d.Id,
                            d.Diagnosis1,
                            d.DiagnosisDate
                        })
                    })
                    .FirstOrDefault();

                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found.");
                }
                return Ok(appointment);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}