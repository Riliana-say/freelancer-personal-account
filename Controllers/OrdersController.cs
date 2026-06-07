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
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public OrdersController(ApplicationDbContext context)
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
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            
            var allOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Executor)
                .ToListAsync();
            
            var result = new List<object>();
            
            foreach (var order in allOrders)
            {
                // Проверка прав: если не админ и не свой заказ - пропускаем
                if (currentUser.Role != "Admin" && order.ExecutorId != currentUser.Id)
                    continue;
                
                result.Add(new
                {
                    order.Id,
                    order.ClientId,
                    ClientName = order.Client?.Name ?? "",
                    ClientPhone = order.Client?.Phone ?? "",
                    order.ServiceName,
                    Price = (double)order.Price,
                    order.Status,
                    ExecutorName = order.Executor?.FullName ?? "Не назначен",
                    order.CreatedAt
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
    public async Task<IActionResult> GetOrder(int id)
    {
        var currentUser = GetCurrentUser();
        if (currentUser == null) return Unauthorized();
        
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Executor)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (order == null) return NotFound();
        
        if (currentUser.Role != "Admin" && order.ExecutorId != currentUser.Id)
            return Forbid();
        
        return Ok(order);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        try
        {
            if (order == null)
                return BadRequest(new { message = "Данные заказа не переданы" });
            
            // Валидация
            if (order.ClientId <= 0)
                return BadRequest(new { message = "Выберите клиента" });
            
            if (string.IsNullOrWhiteSpace(order.ServiceName))
                return BadRequest(new { message = "Укажите название услуги" });
            
            if (order.Price <= 0)
                return BadRequest(new { message = "Укажите корректную стоимость" });
            
            // Проверяем существование клиента
            var clientExists = await _context.Clients.AnyAsync(c => c.Id == order.ClientId);
            if (!clientExists)
                return BadRequest(new { message = "Клиент с таким ID не существует" });
            
            // Если указан исполнитель, проверяем его существование
            if (order.ExecutorId.HasValue && order.ExecutorId.Value > 0)
            {
                var executorExists = await _context.Users.AnyAsync(u => u.Id == order.ExecutorId.Value);
                if (!executorExists)
                    return BadRequest(new { message = "Исполнитель с таким ID не существует" });
            }
            
            var newOrder = new Order
            {
                ClientId = order.ClientId,
                ServiceName = order.ServiceName,
                Price = order.Price,
                Status = "New", // Всегда новый заказ
                ExecutorId = order.ExecutorId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Заказ создан", orderId = newOrder.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ошибка при создании заказа", error = ex.Message });
        }
    }
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        try
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            
            if (currentUser.Role != "Admin" && order.ExecutorId != currentUser.Id)
                return Forbid();
            
            var validStatuses = new[] { "New", "InProgress", "Completed", "Paid" };
            if (!validStatuses.Contains(status))
                return BadRequest(new { message = "Неверный статус" });
            
            order.Status = status;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Статус обновлён" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Заказ удалён" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}