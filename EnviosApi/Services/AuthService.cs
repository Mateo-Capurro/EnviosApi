using System;
using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;

namespace EnviosApi.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _repository;
    private readonly ITokenService _tokenService;

    public AuthService(IUsuarioRepository repository, ITokenService tokenService)
    {
        _repository = repository;
        _tokenService = tokenService;
    }

    public async Task<UsuarioDto> RegisterAsync(RegisterDto dto)
    {
        var email = Normalizar(dto.Email);

        if (await _repository.EmailExistsAsync(email))
            throw new ConflictException($"Ya existe un usuario con el email {email}.");

        // El primer usuario registrado administra el sistema; el resto son operadores.
        var rol = await _repository.AnyAsync() ? "Operador" : "Admin";

        var usuario = await _repository.CreateAsync(new Usuario
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = rol
        });
        return ToDto(usuario);
    }

    public async Task<TokenDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _repository.GetByEmailAsync(Normalizar(dto.Email));

        // Mismo mensaje para email inexistente y contraseña incorrecta: no revela qué emails están registrados.
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            throw new UnauthorizedException("Email o contraseña incorrectos.");

        return _tokenService.GenerarToken(usuario);
    }

    private static string Normalizar(string email) => email.Trim().ToLowerInvariant();

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        Rol = u.Rol
    };
}
