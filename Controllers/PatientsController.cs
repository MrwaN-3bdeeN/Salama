using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salama.Models;

namespace Salama.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult GetPatientHistory(int id)
        {
            try
            {
                var patient = _context.Patients.Find(id);
                if (patient == null)
                {
                    return NotFound($"History of patient with ID {id} not found.");
                }
                return Ok(patient);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}