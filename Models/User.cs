using System.ComponentModel.DataAnnotations;

namespace FreelancerPersonalAccount.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = "Executor";
}
