using System;
using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;

namespace EnviosApi.Services;

public class EnvioService: IEnvioService
{
     private static readonly Dictionary<EstadoEnvio, EstadoEnvio[]> Transiciones = new()
    {
        [EstadoEnvio.Pendiente] = new[] { EstadoEnvio.EnTransito, EstadoEnvio.Cancelado },
        [EstadoEnvio.EnTransito] = new[] { EstadoEnvio.Entregado, EstadoEnvio.Cancelado },
        [EstadoEnvio.Entregado] = Array.Empty<EstadoEnvio>(),
        [EstadoEnvio.Cancelado] = Array.Empty<EstadoEnvio>()
    };
        private readonly IEnvioRepository _repository;

    public EnvioService(IEnvioRepository repository) => _repository = repository;

    public async Task<IEnumerable<EnvioDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto);

    public async Task<EnvioDto> GetByIdAsync(int id) =>
        ToDto(await ObtenerAsync(id));

    public async Task<EnvioDto> CreateAsync(EnvioCreateDto dto)
    {
        if (!await _repository.ClienteExistsAsync(dto.ClienteId))
            throw new NotFoundException($"No existe el cliente con id {dto.ClienteId}.");

        var envio = await _repository.CreateAsync(new Envio
        {
            Direccion = dto.Direccion,
            ClienteId = dto.ClienteId
        });
        return ToDto(envio);
    }

    public async Task<EnvioDto> CambiarEstadoAsync(int id, EstadoEnvio nuevoEstado)
    {
        var envio = await ObtenerAsync(id);

        if (!Transiciones[envio.Estado].Contains(nuevoEstado))
            throw new BusinessRuleException(
                $"No se puede pasar de {envio.Estado} a {nuevoEstado}.");

        envio.Estado = nuevoEstado;
        await _repository.UpdateAsync(envio);
        return ToDto(envio);
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _repository.DeleteAsync(id))
            throw new NotFoundException($"No existe el envío con id {id}.");
    }

    private async Task<Envio> ObtenerAsync(int id) =>
        await _repository.GetByIdAsync(id)
        ?? throw new NotFoundException($"No existe el envío con id {id}.");

    private static EnvioDto ToDto(Envio e) => new()
    {
        Id = e.Id,
        Direccion = e.Direccion,
        Estado = e.Estado,
        FechaCreacion = e.FechaCreacion,
        ClienteId = e.ClienteId
    };

}
