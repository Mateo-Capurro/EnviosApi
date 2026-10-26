using System;
using EnviosApi.DTOs;
using EnviosApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnviosApi.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ClientesController: ControllerBase
{
  private readonly IClienteService _service;

    public ClientesController(IClienteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id) =>
        Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteCreateDto dto)
    {
        var cliente = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Update(int id, ClienteCreateDto dto) =>
        Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

}
