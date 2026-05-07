using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]

    public class ClinicsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ClinicsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult GetAllClinics()
        {
            try
            {
                var clinics = _context.Clinics.ToList();
                if (clinics == null || clinics.Count == 0)
                {
                    return NotFound("No clinics found.");
                }
                return Ok(clinics);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}