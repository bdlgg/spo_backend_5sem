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
        
        //get /api/sales
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            return Ok(sales);
        }
        
        //get /api/sales/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = "Сделка не найдена" });
            return Ok(sale);
        }

        // post /api/sales
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Sale sale)
        {
            var (success, message, createdSale) = await _saleService.CreateAsync(sale);

            if (!success)
                return BadRequest(new { message = message });

            return Ok(new
            {
                message,
                sale = createdSale
            });
        }
        
        //put /api/sales/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Sale sale)
        {
            var updated = await _saleService.UpdateAsync(id, sale);
            if (!updated)
                return NotFound(new { message = "Сделка не найдена" });
            return Ok(new { message = "Сделка успешно обновлена" });
        }

        //delete /api/sales/{id}
        [Authorize(Roles = "Админ")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _saleService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Сделка не найдена" });
            return Ok(new { message = "Сделка успешно удалена" });
        }
        
        //get /api/sales/client/{clientId}
        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetByClientId(int clientId)
        {
            var sales = await _saleService.GetByClientIdAsync(clientId);
            return Ok(sales);
        }
        
        //get /api/sales/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetByUserId(int userId)
        {
            var sales = await _saleService.GetByUserIdAsync(userId);
            return Ok(sales);
        }
    }
}