using System;
using EnviosApi.DTOs;
using EnviosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnviosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioDto>> Register(RegisterDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _service.RegisterAsync(dto));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto) =>
        Ok(await _service.LoginAsync(dto));
}
