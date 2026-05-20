using Microsoft.AspNetCore.Mvc;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using backend_spo.DTOs;


namespace backend_spo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarsController : ControllerBase
    {
        private readonly ICarService _carService;

        public CarsController(ICarService carService)
        {
            _carService = carService;
        }

        //get /api/cars
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> GetAll()
        {
            var cars = await _carService.GetAllAsync();
            var result = cars.Select(MapToDto);
            return Ok(result);
        }

        //get /api/cars/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<CarResponseDto>> GetById(int id)
        {
            var car = await _carService.GetByIdAsync(id);

            if (car == null) return NotFound("Машина не найдена");

            return Ok(MapToDto(car));
        }

        //post /api/cars
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<CarResponseDto>> Create([FromBody] CreateCarDto dto)
        {
            var car = new Car
            {
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Price = dto.Price,
                Vin = dto.Vin,
                Color = dto.Color,
                Type = dto.Type,
                Status = dto.Status,
                SupplierId = dto.SupplierId,
                IsActive = true,
                Photos = dto.PhotoUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select((url, index) => new CarPhoto
                    {
                        Url = url.Trim(),
                        SortOrder = index + 1
                    })
                    .ToList()
            };
            var createdCar = await _carService.CreateAsync(car);
            var fullCar = await _carService.GetByIdAsync(createdCar.Id);
            return CreatedAtAction(nameof(GetById), new { id = createdCar.Id }, MapToDto(fullCar!));
        }

        //put /api/cars/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCarDto dto)
        {
            var car = new Car
            {
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Price = dto.Price,
                Vin = dto.Vin,
                Color = dto.Color,
                Type = dto.Type,
                Status = dto.Status,
                SupplierId = dto.SupplierId,
                IsActive = true,
                Photos = dto.PhotoUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select((url, index) => new CarPhoto
                    {
                        Url = url.Trim(),
                        SortOrder = index + 1
                    })
                    .ToList()
            };
            var result = await _carService.UpdateAsync(id, car);
            if (!result) return NotFound("Машина не найдена");
            var updatedCar = await _carService.GetByIdAsync(id);
            return Ok(MapToDto(updatedCar!));
        }

        // delete /api/cars/{id}
        [Authorize(Roles = "Админ")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _carService.DeleteAsync(id);
            if (!result) return NotFound("Машина не найдена");
            return NoContent();
        }

        //get /api/cars/search?brand=..&model=..&year=..
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> Search(string? brand, string? model, int? year)
        {
            var cars = await _carService.SearchAsync(brand, model, year);
            var result = cars.Select(MapToDto);
            return Ok(result);
        }

        private static CarResponseDto MapToDto(Car car)
        {
            return new CarResponseDto
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Year = car.Year,
                Price = car.Price,
                Vin = car.Vin,
                Color = car.Color,
                Type = car.Type,
                Status = car.Status,
                IsActive = car.IsActive,
                SupplierId = car.SupplierId,
                SupplierName = car.Supplier?.Name ?? string.Empty,
                Photos = car.Photos
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new CarPhotoResponseDto
                    {
                        Id = p.Id,
                        Url = p.Url,
                        SortOrder = p.SortOrder
                    })
                    .ToList()
            };
        }
    }
}