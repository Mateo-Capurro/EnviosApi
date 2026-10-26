using System;
using EnviosApi.Models;

namespace EnviosApi.Repositories;

public interface IEnvioRepository
{
 Task<List<Envio>> GetAllAsync();
    Task<Envio?> GetByIdAsync(int id);
    Task<Envio> CreateAsync(Envio envio);
    Task<bool> DeleteAsync(int id);
    Task<bool> ClienteExistsAsync(int clienteId);
    Task UpdateAsync(Envio envio);
}
