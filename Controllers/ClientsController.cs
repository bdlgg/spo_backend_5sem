using Microsoft.AspNetCore.Mvc;
using backend_spo.Interfaces;
using backend_spo.Entities;
using Microsoft.AspNetCore.Authorization;

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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetAll()
        {
            var clients = await _clientService.GetAllAsync();
            return Ok(clients);
        }
        
        // get /api/clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null) return NotFound("Клиент не найден");
            return Ok(client);
        }
        
        //post /api/clients
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<Client>> Create([FromBody] Client client)
        {
            var createdClient = await _clientService.CreateAsync(client);
            return CreatedAtAction(nameof(GetById), new {id = createdClient.Id}, createdClient);
        }
        
        //put /api/clients/{id}
        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Client client)
        {
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
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Client>>> Search(string? firstName, string? lastName, string? phone)
        {
            var clients = await _clientService.SearchAsync(firstName, lastName, phone);
            return Ok(clients);
        }
    }
}