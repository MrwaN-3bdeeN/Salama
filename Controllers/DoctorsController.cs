using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[Controller]")]
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
                var doctors = _context.Doctors
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        UserName = d.IdNavigation.Name
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
                var doctor = _context.Doctors
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.About,
                        d.Experience,
                        d.SpecializationId,
                        UserName = d.IdNavigation.Name
                    })
                    .FirstOrDefault();
                if (doctor == null)
                {
                    return NotFound($"Doctor with ID {id} not found.");
                }
                return Ok(doctor);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}