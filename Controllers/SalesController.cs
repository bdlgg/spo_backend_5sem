using Microsoft.AspNetCore.Mvc;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using backend_spo.DTOs;
using System.Security.Claims;

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
        public async Task<ActionResult<IEnumerable<SaleResponseDto>>> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            var result = sales.Select(MapToDto);
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<SaleResponseDto>> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = "Сделка не найдена" });
            return Ok(MapToDto(sale));
        }


        [Authorize(Roles = "Админ,Руководитель,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<SaleResponseDto>> Create([FromBody] CreateSaleDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = "Не удалось определить текущего пользователя" });

            var sale = new Sale
            {
                CarId = dto.CarId,
                ClientId = dto.ClientId,
                FinalPrice = dto.FinalPrice,
                Status = dto.Status
            };

            var (success, message, createdSale) = await _saleService.CreateAsync(sale, currentUserId);

            if (!success || createdSale == null)
                return BadRequest(new { message });

            return Ok(MapToDto(createdSale));
        }

        //put /api/sales/{id}
        [Authorize(Roles = "Админ,Руководитель,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSaleDto dto)
        {
            var sale = new Sale
            {
                CarId = dto.CarId,
                ClientId = dto.ClientId,
                FinalPrice = dto.FinalPrice,
                Status = dto.Status
            };

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
        public async Task<ActionResult<IEnumerable<SaleResponseDto>>> GetByClientId(int clientId)
        {
            var sales = await _saleService.GetByClientIdAsync(clientId);
            var result = sales.Select(MapToDto);
            return Ok(result);
        }

        //get /api/sales/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<SaleResponseDto>>> GetByUserId(int userId)
        {
            var sales = await _saleService.GetByUserIdAsync(userId);
            var result = sales.Select(MapToDto);
            return Ok(result);
        }

        private static SaleResponseDto MapToDto(Sale sale)
        {
            return new SaleResponseDto
            {
                Id = sale.Id,
                CarId = sale.CarId,
                ClientId = sale.ClientId,
                UserId = sale.UserId,
                SaleDate = sale.SaleDate,
                FinalPrice = sale.FinalPrice,
                Status = sale.Status
            };
        }
    }
}