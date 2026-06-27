using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClinicsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ClinicsController(AppDbContext context)
        {
            _context = context;
        }

        // FIX: Include Address and Phone in response
        [HttpGet]
        public IActionResult GetAllClinics()
        {
            try
            {
                var clinics = _context.Clinics
                    .Select(c => new
                    {
                        c.Id,
                        c.ClinicName,
                        c.Address,
                        c.Phone,
                        SpecializationName = c.Specialization != null ? c.Specialization.SpecializationName : null
                    })
                    .ToList();

                if (!clinics.Any())
                    return NotFound(new { message = "No clinics found." });

                return Ok(clinics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
