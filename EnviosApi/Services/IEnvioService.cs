using System;
using EnviosApi.DTOs;
using EnviosApi.Models;

namespace EnviosApi.Services;

public interface IEnvioService
{
    Task<IEnumerable<EnvioDto>> GetAllAsync();
    Task<EnvioDto> GetByIdAsync(int id);
    Task<EnvioDto> CreateAsync(EnvioCreateDto dto);
    Task<EnvioDto> CambiarEstadoAsync(int id, EstadoEnvio nuevoEstado);
    Task DeleteAsync(int id);

}
