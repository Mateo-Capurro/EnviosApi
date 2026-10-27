using System.ComponentModel.DataAnnotations;
using EnviosApi.DTOs;

namespace EnviosApi.Tests.DTOs;

// La regla del DNI no está en ClienteService: es un [RegularExpression] del DTO que aplica el model binding.
public class ClienteCreateDtoTests
{
    private static List<ValidationResult> Validar(ClienteCreateDto dto)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);
        return resultados;
    }

    private static ClienteCreateDto NuevoDto(string dni) => new()
    {
        Nombre = "Ana Pérez",
        Dni = dni,
        Email = "ana@mail.com"
    };

    [Theory]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Validar_DniDe7u8Digitos_EsValido(string dni)
    {
        // Arrange
        var dto = NuevoDto(dni);

        // Act
        var errores = Validar(dto);

        // Assert
        Assert.Empty(errores);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789")]
    [InlineData("12.345.678")]
    [InlineData("1234567a")]
    [InlineData(" 1234567")]
    public void Validar_DniConFormatoInvalido_DevuelveErrorDeDni(string dni)
    {
        // Arrange
        var dto = NuevoDto(dni);

        // Act
        var errores = Validar(dto);

        // Assert
        var error = Assert.Single(errores);
        Assert.Contains(nameof(ClienteCreateDto.Dni), error.MemberNames);
        Assert.Equal("El DNI debe tener 7 u 8 dígitos.", error.ErrorMessage);
    }
}
