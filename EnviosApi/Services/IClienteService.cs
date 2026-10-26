using System;
using EnviosApi.DTOs;
namespace EnviosApi.Services;

public interface IClienteService
{
     Task<IEnumerable<ClienteDto>> GetAllAsync();
    Task<ClienteDto> GetByIdAsync(int id);
    Task<ClienteDto> CreateAsync(ClienteCreateDto dto);
    Task<ClienteDto> UpdateAsync(int id, ClienteCreateDto dto);
    Task DeleteAsync(int id);

}
