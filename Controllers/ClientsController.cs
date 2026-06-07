using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreelancerPersonalAccount.Data;
using FreelancerPersonalAccount.Models;
using System.Security.Claims;

namespace FreelancerPersonalAccount.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public ClientsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    private User? GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return null;
        
        var userId = int.Parse(userIdClaim.Value);
        return _context.Users.FirstOrDefault(u => u.Id == userId);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        try
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            
            var allOrders = await _context.Orders.ToListAsync();
            var allClients = await _context.Clients.ToListAsync();
            
            var result = new List<object>();
            
            foreach (var client in allClients)
            {
                var clientOrders = allOrders.Where(o => o.ClientId == client.Id).ToList();
                var ordersCount = clientOrders.Count;
                var totalSum = clientOrders
                    .Where(o => o.Status == "Paid")
                    .Sum(o => (double)o.Price);
                
                result.Add(new
                {
                    client.Id,
                    client.Name,
                    client.Phone,
                    client.Email,
                    OrdersCount = ordersCount,
                    TotalSum = totalSum
                });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) return NotFound();
        
        return Ok(client);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateClient([FromBody] Client client)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Клиент добавлен", clientId = client.Id });
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] Client client)
    {
        if (id != client.Id)
            return BadRequest();
        
        var existingClient = await _context.Clients.FindAsync(id);
        if (existingClient == null) return NotFound();
        
        existingClient.Name = client.Name;
        existingClient.Phone = client.Phone;
        existingClient.Email = client.Email;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Клиент обновлён" });
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) return NotFound();
        
        var hasOrders = await _context.Orders.AnyAsync(o => o.ClientId == id);
        if (hasOrders)
            return BadRequest(new { message = "Нельзя удалить клиента с заказами" });
        
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Клиент удалён" });
    }
}