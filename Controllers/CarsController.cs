using Microsoft.AspNetCore.Mvc;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;


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
        public async Task<ActionResult<IEnumerable<Car>>> GetAll()
        {
            var cars = await _carService.GetAllAsync();
            return Ok(cars);
        }

        //get /api/cars/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Car>> GetById(int id)
        {
            var car = await _carService.GetByIdAsync(id);

            if (car == null) return NotFound("Машина не найдена");

            return Ok(car);
        }

        //post /api/cars
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<Car>> Create([FromBody] Car car)
        {
            var createdCar = await _carService.CreateAsync(car);
            return CreatedAtAction(nameof(GetById), new {id = createdCar.Id}, createdCar);
        }
        
        //put /api/cars/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Car car)
        {
            var result = await _carService.UpdateAsync(id, car);
            if (!result) return NotFound("Машина не найдена");
            return NoContent();
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
        public async Task<ActionResult<IEnumerable<Car>>> Search(string? brand, string? model, int? year)
        {
            var cars = await _carService.SearchAsync(brand, model, year);
            return Ok(cars);
        }
    }
}