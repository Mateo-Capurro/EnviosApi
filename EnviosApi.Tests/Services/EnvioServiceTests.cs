using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;
using EnviosApi.Services;
using Moq;

namespace EnviosApi.Tests.Services;

public class EnvioServiceTests
{
    private readonly Mock<IEnvioRepository> _repository = new();
    private readonly EnvioService _service;

    public EnvioServiceTests() => _service = new EnvioService(_repository.Object);

    private Envio ArrangeEnvio(EstadoEnvio estado)
    {
        var envio = new Envio { Id = 1, Direccion = "Calle 123", Estado = estado, ClienteId = 10 };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(envio);
        return envio;
    }

    [Theory]
    [InlineData(EstadoEnvio.Pendiente, EstadoEnvio.EnTransito)]
    [InlineData(EstadoEnvio.Pendiente, EstadoEnvio.Cancelado)]
    [InlineData(EstadoEnvio.EnTransito, EstadoEnvio.Entregado)]
    [InlineData(EstadoEnvio.EnTransito, EstadoEnvio.Cancelado)]
    public async Task CambiarEstadoAsync_TransicionValida_ActualizaEstadoYPersiste(
        EstadoEnvio actual, EstadoEnvio nuevo)
    {
        // Arrange
        var envio = ArrangeEnvio(actual);

        // Act
        var resultado = await _service.CambiarEstadoAsync(1, nuevo);

        // Assert
        Assert.Equal(nuevo, resultado.Estado);
        Assert.Equal(nuevo, envio.Estado);
        _repository.Verify(r => r.UpdateAsync(envio), Times.Once);
    }

    [Theory]
    [InlineData(EstadoEnvio.Pendiente, EstadoEnvio.Entregado)]
    [InlineData(EstadoEnvio.Pendiente, EstadoEnvio.Pendiente)]
    [InlineData(EstadoEnvio.EnTransito, EstadoEnvio.Pendiente)]
    [InlineData(EstadoEnvio.EnTransito, EstadoEnvio.EnTransito)]
    public async Task CambiarEstadoAsync_TransicionInvalida_LanzaBusinessRuleException(
        EstadoEnvio actual, EstadoEnvio nuevo)
    {
        // Arrange
        var envio = ArrangeEnvio(actual);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CambiarEstadoAsync(1, nuevo));
        Assert.Equal($"No se puede pasar de {actual} a {nuevo}.", ex.Message);
        Assert.Equal(actual, envio.Estado);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoEnvio.Entregado, EstadoEnvio.Pendiente)]
    [InlineData(EstadoEnvio.Entregado, EstadoEnvio.EnTransito)]
    [InlineData(EstadoEnvio.Entregado, EstadoEnvio.Cancelado)]
    [InlineData(EstadoEnvio.Cancelado, EstadoEnvio.Pendiente)]
    [InlineData(EstadoEnvio.Cancelado, EstadoEnvio.EnTransito)]
    [InlineData(EstadoEnvio.Cancelado, EstadoEnvio.Entregado)]
    public async Task CambiarEstadoAsync_DesdeEstadoFinal_LanzaBusinessRuleException(
        EstadoEnvio final, EstadoEnvio nuevo)
    {
        // Arrange
        ArrangeEnvio(final);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CambiarEstadoAsync(1, nuevo));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_EnvioInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Envio?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CambiarEstadoAsync(99, EstadoEnvio.EnTransito));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_EnvioInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Envio?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_ClienteInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.ClienteExistsAsync(42)).ReturnsAsync(false);
        var dto = new EnvioCreateDto { Direccion = "Calle 123", ClienteId = 42 };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        _repository.Verify(r => r.CreateAsync(It.IsAny<Envio>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ClienteExistente_CreaEnvioPendiente()
    {
        // Arrange
        _repository.Setup(r => r.ClienteExistsAsync(42)).ReturnsAsync(true);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Envio>()))
            .ReturnsAsync((Envio e) => { e.Id = 7; return e; });
        var dto = new EnvioCreateDto { Direccion = "Calle 123", ClienteId = 42 };

        // Act
        var resultado = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(7, resultado.Id);
        Assert.Equal(42, resultado.ClienteId);
        Assert.Equal(EstadoEnvio.Pendiente, resultado.Estado);
    }

    [Fact]
    public async Task DeleteAsync_EnvioInexistente_LanzaNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(99)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }
}
