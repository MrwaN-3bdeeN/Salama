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
                var specializations =
                from s in _context.Specializations
                join c in _context.Clinics on s.Id equals c.SpecializationId
                join d in _context.Doctors on s.Id equals d.SpecializationId
                select new
                {
                    s.Id,
                    SpecializationName = s.SpecializationName,
                    ClinicName = c.ClinicName,
                    ClinicId = c.Id,
                    DoctorName = d.IdNavigation.Name,
                    DoctorId = d.Id
                };
                
                
                if (specializations == null || (int)specializations.Count() == 0)
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