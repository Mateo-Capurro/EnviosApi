using System;
using EnviosApi.Models;

namespace EnviosApi.Repositories;

public interface IClienteRepository
{
Task<List<Cliente>> GetAllAsync();
    Task<Cliente?> GetByIdAsync(int id);
    Task<bool> DniExistsAsync(string dni, int? excludeId = null);
    Task<bool> HasEnviosAsync(int id);
    Task<Cliente> CreateAsync(Cliente cliente);
    Task UpdateAsync(Cliente cliente);
    Task DeleteAsync(Cliente cliente);
}
