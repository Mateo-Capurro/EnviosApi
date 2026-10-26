using System.Security.Claims;
using System.Text;
using EnviosApi.Data;
using Microsoft.EntityFrameworkCore;
using EnviosApi.Repositories;
using EnviosApi.Exceptions;
using EnviosApi.Infrastructure;
using EnviosApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
  .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));;
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddScoped<IEnvioService, EnvioService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IEnvioRepository, EnvioRepository>();

// JWT: la clave no está en appsettings, viene de user-secrets (o variable de entorno Jwt__Key en producción).
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Falta la configuración 'Jwt:Key'. En desarrollo ejecutá: dotnet user-secrets set \"Jwt:Key\" \"<clave de al menos 32 caracteres>\"");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("'Jwt:Key' debe tener al menos 32 bytes para firmar con HMAC-SHA256.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Solo en Docker (Database__MigrateOnStartup=true): con SQL Express local las migraciones se aplican a mano.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await MigrarConReintentosAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "EnviosApi v1"));
}
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task MigrarConReintentosAsync(WebApplication app)
{
    const int maxIntentos = 10;
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    for (var intento = 1; ; intento++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas.");
            return;
        }
        catch (Exception ex) when (intento < maxIntentos)
        {
            logger.LogWarning("La base no responde (intento {Intento}/{Max}): {Error}", intento, maxIntentos, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
