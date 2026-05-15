using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]

    public class SpecializationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SpecializationsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult GetAllSpecializations()
        {
            try
            {
                var specializations = _context.Specializations.ToList();
                if (specializations == null || specializations.Count == 0)
                {
                    return NotFound("No specializations found.");
                }
                return Ok(specializations);
            }
        
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



    }    
    
}