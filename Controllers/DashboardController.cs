using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreelancerPersonalAccount.Data;

namespace FreelancerPersonalAccount.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            // Получаем все заказы в память (чтобы использовать LINQ to Objects)
            var allOrders = await _context.Orders.ToListAsync();
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            
            // Заказы за месяц
            var ordersThisMonth = allOrders.Count(o => o.CreatedAt >= startOfMonth);
            
            // Доход за месяц (оплаченные) - используем double для суммы
            var incomeThisMonth = (double)allOrders
                .Where(o => o.CreatedAt >= startOfMonth && o.Status == "Paid")
                .Sum(o => (double)o.Price);
            
            // Заказы в работе
            var ordersInProgress = allOrders.Count(o => o.Status == "InProgress");
            
            // Средний чек
            var paidOrders = allOrders.Where(o => o.Status == "Paid").ToList();
            var averageCheck = paidOrders.Count > 0 ? paidOrders.Average(o => (double)o.Price) : 0;
            
            // Топ-5 услуг по доходу
            var topServices = paidOrders
                .GroupBy(o => o.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    TotalIncome = g.Sum(o => (double)o.Price),
                    OrdersCount = g.Count()
                })
                .OrderByDescending(x => x.TotalIncome)
                .Take(5)
                .ToList();
            
            return Ok(new
            {
                ordersThisMonth,
                incomeThisMonth,
                ordersInProgress,
                averageCheck,
                topServices
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}