using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;
using EnviosApi.Services;
using Moq;

namespace EnviosApi.Tests.Services;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _repository = new();
    private readonly ClienteService _service;

    public ClienteServiceTests() => _service = new ClienteService(_repository.Object);

    private static ClienteCreateDto NuevoDto(string dni = "12345678") => new()
    {
        Nombre = "Ana Pérez",
        Dni = dni,
        Email = "ana@mail.com"
    };

    [Fact]
    public async Task CreateAsync_DniDuplicado_LanzaConflictException()
    {
        // Arrange
        _repository.Setup(r => r.DniExistsAsync("12345678", null)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(NuevoDto()));
        _repository.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DniNuevo_CreaCliente()
    {
        // Arrange
        _repository.Setup(r => r.DniExistsAsync("12345678", null)).ReturnsAsync(false);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Cliente>()))
            .ReturnsAsync((Cliente c) => { c.Id = 5; return c; });

        // Act
        var resultado = await _service.CreateAsync(NuevoDto());

        // Assert
        Assert.Equal(5, resultado.Id);
        Assert.Equal("12345678", resultado.Dni);
        _repository.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DniDeOtroCliente_LanzaConflictException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Cliente { Id = 1, Dni = "11111111" });
        _repository.Setup(r => r.DniExistsAsync("12345678", 1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(1, NuevoDto()));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ClienteInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, NuevoDto()));
    }

    [Fact]
    public async Task DeleteAsync_ClienteConEnvios_LanzaConflictException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Cliente { Id = 1 });
        _repository.Setup(r => r.HasEnviosAsync(1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ClienteSinEnvios_EliminaCliente()
    {
        // Arrange
        var cliente = new Cliente { Id = 1 };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cliente);
        _repository.Setup(r => r.HasEnviosAsync(1)).ReturnsAsync(false);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _repository.Verify(r => r.DeleteAsync(cliente), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ClienteInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }
}
