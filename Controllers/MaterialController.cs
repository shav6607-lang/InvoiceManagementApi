using Microsoft.AspNetCore.Mvc;
using InvoiceManagementApi.Models;
using InvoiceManagementApi.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InvoiceManagementApi.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class MaterialController : BaseController
    {
        private readonly IMaterialRepository _repo;
        private readonly ILogger<MaterialController> _logger;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public MaterialController(IMaterialRepository repo, ILogger<MaterialController> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _repo = repo;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var data = await _repo.GetCompaniesAsync(CurrentUserId);
                return Ok(new { Message = "Success", Data = data });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new { Message = ex.Message, Detail = ex.StackTrace });
                }
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? companyId, [FromQuery] int? materialId)
        {
            try
            {
                var data = await _repo.GetMaterialsAsync(companyId, materialId);
                return Ok(new { Message = "Success", Data = data });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting materials");
                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new { Message = ex.Message, Detail = ex.StackTrace });
                }
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddMaterial([FromBody] MaterialRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            try
            {
                var result = await _repo.InsertOrUpdateMaterialAsync(request);
                return Ok(new { Message = "Success", Affected = result.Affected, ResultMessage = result.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error upserting material");
                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new { Message = ex.Message, Detail = ex.StackTrace });
                }
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }
    }
}
