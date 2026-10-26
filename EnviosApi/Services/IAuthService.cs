using System;
using EnviosApi.DTOs;

namespace EnviosApi.Services;

public interface IAuthService
{
    Task<UsuarioDto> RegisterAsync(RegisterDto dto);
    Task<TokenDto> LoginAsync(LoginDto dto);
}
