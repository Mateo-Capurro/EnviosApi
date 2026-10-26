using EnviosApi.DTOs;
using EnviosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnviosApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EnviosController : ControllerBase
{
    private readonly IEnvioService _service;

    public EnviosController(IEnvioService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnvioDto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnvioDto>> GetById(int id) =>
        Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<EnvioDto>> Create(EnvioCreateDto dto)
    {
        var envio = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = envio.Id }, envio);
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<EnvioDto>> CambiarEstado(int id, EnvioEstadoDto dto) =>
        Ok(await _service.CambiarEstadoAsync(id, dto.Estado));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}