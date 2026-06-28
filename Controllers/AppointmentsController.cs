using System.Data.Common;
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

        [HttpPost("book")]
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


        [HttpGet("{id}/info")]
        public IActionResult GetAppointmentInfo(int id)
        {
            try
            {
                var appointment =
                (
                    from a in _context.Appointments
                    join dc in _context.Doctors on a.DoctorId equals dc.Id
                    join p in _context.Patients on a.PatientId equals p.Id
                    join d in _context.Diagnoses on p.DiagnosisId equals d.Id
                    join d_c in _context.DoctorClinics on d.Id equals d_c.DoctorId
                    join c in _context.Clinics on d_c.ClinicId equals c.Id
                    where a.Id == id
                    select new
                    {
                        a.Id,
                        a.AppintmentDate,
                        a.AppointmentOrder,
                        a.AppointmentStatus,
                        DoctorName = a.Doctor.IdNavigation.Name,
                        PatientName = a.Patient.IdNavigation.Name,
                        c.ClinicName,
                        d.Diagnosis1,
                        d.DiagnosisDate,
                        a.PatientId,
                        a.DoctorId
                    }
                ).FirstOrDefault();
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found.");
                }


                DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
                var remainingTime = appointmentDateTime - DateTime.Now;
                if (remainingTime.TotalHours < 0)
                {
                    return Ok(new { Message = "Old appointment" });
                }
                else if (remainingTime.TotalHours > 0)
                {
                    return Ok(new { Message = "Upcoming appointment" });
                }
                else if (remainingTime.TotalHours > 72)
                {
                    return Ok(new { Message = "Upcoming in 3 days - can be modified.", Appointment = appointment });
                }

                return Ok(appointment);
                

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{specializationId}/history")]
        public IActionResult GetAppointmentBySpecialization(int specializationId)
        {
            try
            {
                var specializationHistory =
                from s in _context.Specializations
                join d in _context.Doctors on s.Id equals d.SpecializationId
                join a in _context.Appointments on d.Id equals a.DoctorId
                where s.Id == specializationId
                select new
                {
                    a.Id,
                    a.AppintmentDate,
                    a.AppointmentOrder,
                    a.AppointmentStatus,
                    DoctorName = d.IdNavigation.Name,
                    diagnosises = a.Diagnoses.Select(d => new
                    {
                        d.Id,
                        d.Diagnosis1,
                        d.DiagnosisDate
                    })
                };


                return Ok(specializationHistory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        [HttpPut("{id}/update")]
        public IActionResult UpdatePatientAppointment(int id)
        {
            try
            {
                var appointment = _context.Appointments.FirstOrDefault(a => a.Id == id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with id {id} not found.");
                }


                DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
                var remainingTime = appointmentDateTime - DateTime.Now;
                if (remainingTime.TotalHours < 72)
                {
                    return BadRequest("Cannot update appointments within 72 hours of the scheduled time.");
                }
                else if (appointment.AppintmentDate < DateOnly.FromDateTime(DateTime.Now))
                {
                    return BadRequest("Cannot update past appointments.");
                }
                else
                {
                    var existingAppointment = _context.Appointments.FirstOrDefault(a => a.Id == id);
                    if (existingAppointment != null)
                    {
                        appointment.AppintmentDate = existingAppointment.AppintmentDate;
                    }
                    _context.SaveChanges();
                    return Ok("Appointment updated successfully.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpDelete("{id}/delete")]
        public IActionResult CancelAppointment(int id)
        {
            try
            {
               var appointment = _context.Appointments.FirstOrDefault(a => a.Id == id);
               if (appointment == null)
                   return NotFound($"Appointment with id {id} not found.");
               

               DateTime appointmentDateTime = appointment.AppintmentDate.ToDateTime(TimeOnly.MinValue);
               var remainingTime = appointmentDateTime - DateTime.Now;
               if (remainingTime.TotalHours < 72)
                    return BadRequest("Cannot cancel appointments within 72 hours of the scheduled time.");
            
                else if (appointment.AppintmentDate < DateOnly.FromDateTime(DateTime.Now))
                    return BadRequest("Cannot cancel past appointments.");
                
                else
                    _context.Appointments.Remove(appointment);
                    _context.SaveChanges();
                    return Ok("Appointment cancelled successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}