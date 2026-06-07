using Microsoft.EntityFrameworkCore;
using FreelancerPersonalAccount.Models;

namespace FreelancerPersonalAccount.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        // Тестовый администратор
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FullName = "Администратор",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin"
            },
            new User
            {
                Id = 2,
                FullName = "Иван Иванов",
                Email = "executor@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("executor123"),
                Role = "Executor"
            }
        );
        
        // Тестовые клиенты
        modelBuilder.Entity<Client>().HasData(
            new Client { Id = 1, Name = "ООО Ромашка", Phone = "+7 (999) 123-45-67", Email = "info@romashka.ru" },
            new Client { Id = 2, Name = "ИП Петров", Phone = "+7 (988) 765-43-21", Email = "petrov@mail.ru" },
            new Client { Id = 3, Name = "ООО ТехноСервис", Phone = "+7 (495) 123-45-67", Email = "tech@service.ru" }
        );
        
        // Тестовые заказы
        modelBuilder.Entity<Order>().HasData(
            new Order { Id = 1, ClientId = 1, ServiceName = "Сайт-визитка", Price = 25000, Status = "Paid", ExecutorId = 2, CreatedAt = new DateTime(2024, 1, 15) },
            new Order { Id = 2, ClientId = 1, ServiceName = "Интернет-магазин", Price = 150000, Status = "InProgress", ExecutorId = 2, CreatedAt = new DateTime(2024, 2, 1) },
            new Order { Id = 3, ClientId = 2, ServiceName = "Вёрстка лендинга", Price = 35000, Status = "Completed", ExecutorId = 2, CreatedAt = new DateTime(2024, 1, 20) },
            new Order { Id = 4, ClientId = 3, ServiceName = "Доработка 1С", Price = 80000, Status = "New", ExecutorId = null, CreatedAt = new DateTime(2024, 2, 10) },
            new Order { Id = 5, ClientId = 2, ServiceName = "Мобильное приложение", Price = 250000, Status = "Paid", ExecutorId = 1, CreatedAt = new DateTime(2024, 1, 5) }
        );
    }
}