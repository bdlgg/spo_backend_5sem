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
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        //get /api/clients
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetAll()
        {
            var clients = await _clientService.GetAllAsync();
            var result = clients.Select(MapToDto);
            return Ok(result);
        }

        // get /api/clients/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientResponseDto>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null) return NotFound("Клиент не найден");
            return Ok(MapToDto(client));
        }
        
        //post /api/clients
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<ClientResponseDto>> Create([FromBody] CreateClientDto dto)
        {
            var client = new Client
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email
            };
            var createdClient = await _clientService.CreateAsync(client);
            return CreatedAtAction(nameof(GetById), new {id = createdClient.Id}, MapToDto(createdClient));
        }
        
        //put /api/clients/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateClientDto dto)
        {
            var client = new Client
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email
            };
            var result = await _clientService.UpdateAsync(id, client);
            if (!result) return NotFound("Клиент не найден");
            return NoContent();
        }
        
        //delete /api/clients/{id}
        [Authorize(Roles = "Админ")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _clientService.DeleteAsync(id);
            if (!result) return NotFound("Клиент не найден");
            return NoContent();
        }

        //get /api/clients/search?firstName=..&lastName=..&phone=..
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> Search(string? firstName, string? lastName, string? phone)
        {
            var clients = await _clientService.SearchAsync(firstName, lastName, phone);
            var result = clients.Select(MapToDto);
            return Ok(result);
        }
        
        //вспомогательный метод чтобы не прописывать везде преобразование Client => ClientResponseDto
        private static ClientResponseDto MapToDto(Client client)
        {
            return new ClientResponseDto
            {
                Id = client.Id,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Phone = client.Phone,
                Email = client.Email,
                UserId = client.UserId
            };
        }
    }
}