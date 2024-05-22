using Microsoft.AspNetCore.Mvc;

using MotoComparisonWebAPI.Services;

using System.Threading.Tasks;

namespace MotoComparisonWebAPI.Controllers
{
    

    [Route("api/[controller]")]
    [ApiController]
    public class MotorcycleController : ControllerBase
    {
        private readonly MotorcycleSpecService _motorcycleSpecService;

        public MotorcycleController(MotorcycleSpecService motorcycleSpecService)
        {
            _motorcycleSpecService = motorcycleSpecService;
        }

        [HttpPost("FetchData")]
        public async Task<IActionResult> FetchData()
        {
            await _motorcycleSpecService.FetchAndStoreData();
            return Ok("Data fetch triggered successfully.");
        }

        [HttpPost("FetchDataByManufacturer")]
        public async Task<IActionResult> FetchDataByManufacturer([FromBody] string manufacturer)
        {
            if (string.IsNullOrEmpty(manufacturer))
            {
                return BadRequest("Manufacturer name cannot be empty.");
            }

            await _motorcycleSpecService.FetchAndStoreDataByManufacturer(manufacturer);
            return Ok($"Data fetch for {manufacturer} triggered successfully.");
        }

        [HttpPost("FetchDataForModels")]
        public async Task<IActionResult> FetchDataForModels()
        {

            await _motorcycleSpecService.FetchDataForModels();
            return Ok($"Data fetch for models triggered successfully.");
        }

    }

}
