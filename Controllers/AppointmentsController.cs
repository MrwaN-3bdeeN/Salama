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
        public AppointmentsController (AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult BookAppointment(Appointment appointment)
        {
            try
            {
                _context.Appointments.Add(appointment);
                _context.SaveChanges();
                return Ok("Your appointment has been booked successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}