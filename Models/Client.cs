using System.ComponentModel.DataAnnotations;

namespace FreelancerPersonalAccount.Models;

public class Client
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
