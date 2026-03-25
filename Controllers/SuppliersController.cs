using Microsoft.AspNetCore.Mvc;
using backend_spo.Interfaces;
using backend_spo.Entities;
using Microsoft.AspNetCore.Authorization;
using backend_spo.DTOs;

namespace backend_spo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        //get /api/suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> GetAll()
        {
            var suppliers = await _supplierService.GetAllAsync();
            var result = suppliers.Select(MapToDto);
            return Ok(result);
        }

        //get /api/suppliers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierResponseDto>> GetById(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null) return NotFound("Поставщик не найден");
            return Ok(MapToDto(supplier));
        }

        //post /api/suppliers
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<SupplierResponseDto>> Create([FromBody] CreateSupplierDto dto)
        {
            var supplier = new Supplier
            {
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address
            };

            var createdSupplier = await _supplierService.CreateAsync(supplier);
            return CreatedAtAction(nameof(GetById), new { id = createdSupplier.Id }, MapToDto(createdSupplier));
        }

        //put /api/suppliers/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSupplierDto dto)
        {
            var supplier = new Supplier
            {
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address
            };

            var result = await _supplierService.UpdateAsync(id, supplier);
            if (!result) return NotFound("Поставщик не найден");
            return NoContent();
        }

        //delete /api/suppliers/{id}
        [Authorize(Roles = "Админ")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _supplierService.DeleteAsync(id);

            if (!result.Success)
            {
                if (result.Message == "Поставщик не найден")
                    return NotFound(result.Message);

                return BadRequest(result.Message);
            }

            return NoContent();
        }

        //get /api/suppliers/search?name=..&contactPerson=..&phone=..
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> Search(string? name, string? contactPerson, string? phone)
        {
            var suppliers = await _supplierService.SearchAsync(name, contactPerson, phone);
            var result = suppliers.Select(MapToDto);
            return Ok(result);
        }

        private static SupplierResponseDto MapToDto(Supplier supplier)
        {
            return new SupplierResponseDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address
            };
        }
    }
}