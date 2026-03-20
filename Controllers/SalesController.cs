using Microsoft.AspNetCore.Mvc;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace backend_spo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            return Ok(sales);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale == null)
                return NotFound(new { message = "Продажа не найдена" });

            return Ok(sale);
        }

        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Sale sale)
        {
            var result = await _saleService.CreateAsync(sale);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                message = result.Message,
                sale = result.sale
            });
        }

        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Sale sale)
        {
            var updated = await _saleService.UpdateAsync(id, sale);

            if (!updated)
                return NotFound(new { message = "Продажа не найдена" });

            return Ok(new { message = "Продажа успешно обновлена" });
        }

        [Authorize(Roles = "Админ")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _saleService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Продажа не найдена" });

            return Ok(new { message = "Продажа успешно удалена" });
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetByClientId(int clientId)
        {
            var sales = await _saleService.GetByClientIdAsync(clientId);
            return Ok(sales);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var sales = await _saleService.GetByUserIdAsync(userId);
            return Ok(sales);
        }
    }
}