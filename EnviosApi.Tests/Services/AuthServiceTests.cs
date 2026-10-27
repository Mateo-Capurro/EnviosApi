using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;
using EnviosApi.Services;
using Moq;

namespace EnviosApi.Tests.Services;

public class AuthServiceTests
{
    private const string Password = "Secreta123";
    private const string MensajeLoginInvalido = "Email o contraseña incorrectos.";

    private readonly Mock<IUsuarioRepository> _repository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _repository.Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => { u.Id = 1; return u; });
        _service = new AuthService(_repository.Object, _tokenService.Object);
    }

    private static Usuario UsuarioConPassword(string password) => new()
    {
        Id = 1,
        Email = "ana@mail.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Rol = "Operador"
    };

    [Theory]
    [InlineData(false, "Admin")]
    [InlineData(true, "Operador")]
    public async Task RegisterAsync_SegunSiHayUsuariosPrevios_AsignaRol(bool hayUsuarios, string rolEsperado)
    {
        // Arrange
        _repository.Setup(r => r.AnyAsync()).ReturnsAsync(hayUsuarios);

        // Act
        var resultado = await _service.RegisterAsync(new RegisterDto { Email = "ana@mail.com", Password = Password });

        // Assert
        Assert.Equal(rolEsperado, resultado.Rol);
        _repository.Verify(r => r.CreateAsync(It.Is<Usuario>(u => u.Rol == rolEsperado)), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailDuplicado_LanzaConflictException()
    {
        // Arrange
        _repository.Setup(r => r.EmailExistsAsync("ana@mail.com")).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(new RegisterDto { Email = "ana@mail.com", Password = Password }));
        _repository.Verify(r => r.CreateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Theory]
    [InlineData("  Ana@Mail.COM  ")]
    [InlineData("ANA@MAIL.COM")]
    [InlineData("ana@mail.com\t")]
    public async Task RegisterAsync_EmailConEspaciosYMayusculas_GuardaEmailNormalizado(string email)
    {
        // Arrange
        _repository.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var resultado = await _service.RegisterAsync(new RegisterDto { Email = email, Password = Password });

        // Assert
        Assert.Equal("ana@mail.com", resultado.Email);
        _repository.Verify(r => r.EmailExistsAsync("ana@mail.com"), Times.Once);
        _repository.Verify(r => r.CreateAsync(It.Is<Usuario>(u => u.Email == "ana@mail.com")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_PasswordValida_GuardaHashBcryptYNoTextoPlano()
    {
        // Arrange
        Usuario? creado = null;
        _repository.Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => creado = u)
            .ReturnsAsync((Usuario u) => u);

        // Act
        await _service.RegisterAsync(new RegisterDto { Email = "ana@mail.com", Password = Password });

        // Assert
        Assert.NotNull(creado);
        Assert.NotEqual(Password, creado.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, creado.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_CredencialesCorrectas_DevuelveToken()
    {
        // Arrange
        var usuario = UsuarioConPassword(Password);
        var token = new TokenDto { Token = "jwt-falso", ExpiraEn = DateTime.UtcNow.AddHours(1) };
        _repository.Setup(r => r.GetByEmailAsync("ana@mail.com")).ReturnsAsync(usuario);
        _tokenService.Setup(t => t.GenerarToken(usuario)).Returns(token);

        // Act
        var resultado = await _service.LoginAsync(new LoginDto { Email = " ANA@mail.com ", Password = Password });

        // Assert
        Assert.Same(token, resultado);
        _tokenService.Verify(t => t.GenerarToken(usuario), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_LanzaUnauthorizedException()
    {
        // Arrange
        _repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginDto { Email = "nadie@mail.com", Password = Password }));
        Assert.Equal(MensajeLoginInvalido, ex.Message);
        _tokenService.Verify(t => t.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecta_LanzaUnauthorizedException()
    {
        // Arrange
        _repository.Setup(r => r.GetByEmailAsync("ana@mail.com")).ReturnsAsync(UsuarioConPassword(Password));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginDto { Email = "ana@mail.com", Password = "otra-clave" }));
        Assert.Equal(MensajeLoginInvalido, ex.Message);
        _tokenService.Verify(t => t.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistenteYPasswordIncorrecta_MismoMensaje()
    {
        // Arrange
        _repository.Setup(r => r.GetByEmailAsync("ana@mail.com")).ReturnsAsync(UsuarioConPassword(Password));
        _repository.Setup(r => r.GetByEmailAsync("nadie@mail.com")).ReturnsAsync((Usuario?)null);

        // Act
        var sinUsuario = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginDto { Email = "nadie@mail.com", Password = Password }));
        var malaPassword = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginDto { Email = "ana@mail.com", Password = "otra-clave" }));

        // Assert
        Assert.Equal(sinUsuario.Message, malaPassword.Message);
    }
}
