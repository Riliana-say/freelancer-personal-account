using Microsoft.AspNetCore.Mvc;
using FreelancerPersonalAccount.Models;
using FreelancerPersonalAccount.Services;

namespace FreelancerPersonalAccount.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = await _authService.Register(request);
        
        if (user == null)
            return BadRequest(new { message = "Пользователь с таким email уже существует" });
        
        return Ok(new { message = "Регистрация успешна", userId = user.Id });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _authService.Login(request);
        
        if (token == null)
            return Unauthorized(new { message = "Неверный email или пароль" });
        
        return Ok(new { token });
    }
}