using System.Security.Claims;
using EnviosApi.Models;
using EnviosApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EnviosApi.Tests.Services;

public class TokenServiceTests
{
    private static readonly Usuario Usuario = new() { Id = 42, Email = "ana@mail.com", Rol = "Admin" };

    private static TokenService CrearServicio(int? expireMinutes = null)
    {
        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-prueba-con-mas-de-32-bytes-para-hmac-sha256",
            ["Jwt:Issuer"] = "EnviosApi",
            ["Jwt:Audience"] = "EnviosApiClients"
        };
        if (expireMinutes is not null)
            valores["Jwt:ExpireMinutes"] = expireMinutes.ToString();

        var config = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new TokenService(config);
    }

    [Fact]
    public void GenerarToken_UsuarioValido_IncluyeClaimsSubEmailYRole()
    {
        // Arrange
        var service = CrearServicio(60);

        // Act
        var resultado = service.GenerarToken(Usuario);

        // Assert
        var jwt = new JsonWebToken(resultado.Token);
        Assert.Equal("42", jwt.GetClaim(JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("ana@mail.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Admin", jwt.GetClaim(ClaimTypes.Role).Value);
        Assert.Equal("EnviosApi", jwt.Issuer);
        Assert.Contains("EnviosApiClients", jwt.Audiences);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(60)]
    [InlineData(120)]
    public void GenerarToken_ConExpireMinutesConfigurado_ExpiraEnEseTiempo(int minutos)
    {
        // Arrange
        var service = CrearServicio(minutos);
        var antes = DateTime.UtcNow;

        // Act
        var resultado = service.GenerarToken(Usuario);

        // Assert
        var despues = DateTime.UtcNow;
        Assert.InRange(resultado.ExpiraEn, antes.AddMinutes(minutos), despues.AddMinutes(minutos));

        // El claim exp del JWT se guarda en segundos enteros: se compara con 1 s de tolerancia.
        var jwt = new JsonWebToken(resultado.Token);
        Assert.Equal(resultado.ExpiraEn, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerarToken_SinExpireMinutes_ExpiraEn60MinutosPorDefecto()
    {
        // Arrange
        var service = CrearServicio();
        var antes = DateTime.UtcNow;

        // Act
        var resultado = service.GenerarToken(Usuario);

        // Assert
        Assert.InRange(resultado.ExpiraEn, antes.AddMinutes(60), DateTime.UtcNow.AddMinutes(60));
    }
}
