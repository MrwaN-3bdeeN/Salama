using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[controller]")]
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
            public int? ClinicId { get; set; }
            public string? Diagnosis { get; set; }
            public DateOnly AppointmentDate { get; set; }
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
        {
            try
            {
                var newId = (_context.Appointments.Any() ? _context.Appointments.Max(a => a.Id) : 0) + 1;
                var order = (_context.Appointments
                    .Any(a => a.DoctorId == request.DoctorId && a.AppintmentDate == request.AppointmentDate)
                    ? _context.Appointments
                        .Where(a => a.DoctorId == request.DoctorId && a.AppintmentDate == request.AppointmentDate)
                        .Max(a => a.AppointmentOrder)
                    : 0) + 1;

                var appointment = new Appointment
                {
                    Id = newId,
                    DoctorId = request.DoctorId,
                    PatientId = request.PatientId,
                    ClinicId = request.ClinicId,
                    AppintmentDate = request.AppointmentDate,
                    AppointmentOrder = order,
                    AppointmentStatus = "Scheduled"
                };

                _context.Appointments.Add(appointment);

                if (!string.IsNullOrEmpty(request.Diagnosis))
                {
                    var diagnosisId = (_context.Diagnoses.Any() ? _context.Diagnoses.Max(d => d.Id) : 0) + 1;
                    _context.Diagnoses.Add(new Diagnosis
                    {
                        Id = diagnosisId,
                        PatientId = request.PatientId,
                        DoctorId = request.DoctorId,
                        AppointmentId = newId,
                        DiagnosisDate = request.AppointmentDate,
                        Diagnosis1 = request.Diagnosis
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Appointment booked successfully.", appointmentId = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/info")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                        DoctorId = a.DoctorId,
                        PatientName = a.Patient.IdNavigation.Name,
                        PatientId = a.PatientId,
                        ClinicName = a.Clinic != null ? a.Clinic.ClinicName : null,
                        ClinicId = a.ClinicId,
                        Diagnoses = a.Diagnoses.Select(d => new
                        {
                            d.Id,
                            d.Diagnosis1,
                            d.DiagnosisDate
                        }).ToList()
                    })
                    .FirstOrDefault();

                if (appointment == null)
                    return NotFound(new { message = $"Appointment with ID {id} not found." });

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-specialization/{specializationId}")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        public IActionResult GetAppointmentBySpecialization(int specializationId)
        {
            try
            {
                var history = _context.Appointments
                    .Where(a => a.Doctor.SpecializationId == specializationId)
                    .Select(a => new
                    {
                        a.Id,
                        a.AppintmentDate,
                        a.AppointmentOrder,
                        a.AppointmentStatus,
                        DoctorName = a.Doctor.IdNavigation.Name,
                        Diagnoses = a.Diagnoses.Select(d => new
                        {
                            d.Id,
                            d.Diagnosis1,
                            d.DiagnosisDate
                        }).ToList()
                    })
                    .ToList();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // FIX: Accept new date in request body and actually update it
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdatePatientAppointment(int id, [FromBody] UpdateAppointmentRequest request)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound(new { message = $"Appointment with id {id} not found." });

                if (appointment.AppointmentStatus == "Completed" || appointment.AppointmentStatus == "Cancelled")
                    return BadRequest(new { message = "Cannot update completed or cancelled appointments." });

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
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // FIX: Add braces around else block
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound(new { message = $"Appointment with id {id} not found." });

                if (appointment.AppointmentStatus == "Completed" || appointment.AppointmentStatus == "Cancelled")
                    return BadRequest(new { message = "Appointment is already completed or cancelled." });

                DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
                var remainingTime = appointmentDateTime - DateTime.Now;

                if (remainingTime.TotalHours < 72)
                    return BadRequest(new { message = "Cannot cancel appointments within 72 hours of the scheduled time." });

                if (appointment.AppintmentDate < DateOnly.FromDateTime(DateTime.Now))
                    return BadRequest(new { message = "Cannot cancel past appointments." });

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Appointment cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class UpdateAppointmentRequest
        {
            public DateOnly NewDate { get; set; }
        }
    }
}
