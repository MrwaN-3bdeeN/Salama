using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpecializationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SpecializationsController(AppDbContext context)
        {
            _context = context;
        }

        // FIX: Use nested Select to avoid Cartesian product
        [HttpGet]
        public IActionResult GetAllSpecializations()
        {
            try
            {
                var specializations = _context.Specializations
                    .Select(s => new
                    {
                        s.Id,
                        s.SpecializationName,
                        Clinics = s.Clinics.Select(c => new
                        {
                            c.Id,
                            c.ClinicName
                        }).ToList(),
                        Doctors = s.Doctors.Select(d => new
                        {
                            d.Id,
                            Name = d.IdNavigation.Name
                        }).ToList()
                    })
                    .ToList();

                if (!specializations.Any())
                    return NotFound(new { message = "No specializations found." });

                return Ok(specializations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
