using Microsoft.EntityFrameworkCore;
using FreelancerPersonalAccount.Models;

namespace FreelancerPersonalAccount.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Проверяем, есть ли уже данные
        if (await context.Orders.AnyAsync())
            return;
        
        // 1. Добавляем пользователей
        var users = new List<User>
        {
            new() { Id = 1, FullName = "Администратор", Email = "admin@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "Admin" },
            new() { Id = 2, FullName = "Иван Иванов", Email = "executor@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("executor123"), Role = "Executor" },
            new() { Id = 3, FullName = "Мария Петрова", Email = "maria@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("maria123"), Role = "Executor" }
        };
        
        // 2. Добавляем клиентов
        var clients = new List<Client>
        {
            new() { Id = 1, Name = "ООО Ромашка", Phone = "+7 (999) 123-45-67", Email = "info@romashka.ru" },
            new() { Id = 2, Name = "ИП Петров", Phone = "+7 (988) 765-43-21", Email = "petrov@mail.ru" },
            new() { Id = 3, Name = "ООО ТехноСервис", Phone = "+7 (495) 123-45-67", Email = "tech@service.ru" },
            new() { Id = 4, Name = "Студия Дизайна", Phone = "+7 (812) 123-45-67", Email = "hello@design.ru" },
            new() { Id = 5, Name = "ИП Смирнов", Phone = "+7 (921) 555-55-55", Email = "smirnov@mail.ru" }
        };
        
        // 3. Добавляем заказы
        var orders = new List<Order>
        {
            new() { Id = 1, ClientId = 1, ServiceName = "Сайт-визитка", Price = 25000, Status = "Paid", ExecutorId = 2, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new() { Id = 2, ClientId = 1, ServiceName = "Интернет-магазин", Price = 150000, Status = "InProgress", ExecutorId = 2, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new() { Id = 3, ClientId = 2, ServiceName = "Вёрстка лендинга", Price = 35000, Status = "Completed", ExecutorId = 2, CreatedAt = DateTime.UtcNow.AddDays(-25) },
            new() { Id = 4, ClientId = 3, ServiceName = "Доработка 1С", Price = 80000, Status = "New", ExecutorId = null, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 5, ClientId = 2, ServiceName = "Мобильное приложение", Price = 250000, Status = "Paid", ExecutorId = 1, CreatedAt = DateTime.UtcNow.AddDays(-40) },
            new() { Id = 6, ClientId = 4, ServiceName = "Корпоративный портал", Price = 300000, Status = "InProgress", ExecutorId = 3, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new() { Id = 7, ClientId = 4, ServiceName = "SEO оптимизация", Price = 45000, Status = "Paid", ExecutorId = 2, CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new() { Id = 8, ClientId = 5, ServiceName = "CRM система", Price = 180000, Status = "New", ExecutorId = null, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = 9, ClientId = 3, ServiceName = "Обновление сайта", Price = 30000, Status = "Completed", ExecutorId = 3, CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new() { Id = 10, ClientId = 1, ServiceName = "Контекстная реклама", Price = 55000, Status = "Paid", ExecutorId = 2, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new() { Id = 11, ClientId = 5, ServiceName = "Чат-бот для Telegram", Price = 70000, Status = "InProgress", ExecutorId = 1, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 12, ClientId = 2, ServiceName = "Интеграция с 1С", Price = 95000, Status = "New", ExecutorId = null, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        
        await context.Users.AddRangeAsync(users);
        await context.Clients.AddRangeAsync(clients);
        await context.Orders.AddRangeAsync(orders);
        
        await context.SaveChangesAsync();
    }
}