using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreelancerPersonalAccount.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [ForeignKey("ClientId")]
    public virtual Client? Client { get; set; }
    
    [Required]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    [Required]
    public string Status { get; set; } = "New";
    
    public int? ExecutorId { get; set; }
    
    [ForeignKey("ExecutorId")]
    public virtual User? Executor { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}