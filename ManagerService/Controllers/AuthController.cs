using ManagerService.Auth;
using ManagerService.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagerService.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(TokenService tokens, ILogger<AuthController> logger)
    {
        _tokens = tokens;
        _logger = logger;
    }

    [HttpPost("login")]
    public ActionResult<TokenDto> Login([FromBody] LoginDto dto)
    {
        var token = _tokens.Create(dto.Username, dto.Password);
        if (token is null)
        {
            _logger.LogWarning("Rejected login for {Username}", dto.Username);
            return Unauthorized();
        }

        _logger.LogInformation("Issued token for {Username}", dto.Username);
        return new TokenDto(token);
    }
}
