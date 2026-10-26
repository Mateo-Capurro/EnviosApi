using System;
using EnviosApi.Data;
using EnviosApi.Models;
using Microsoft.EntityFrameworkCore;
namespace EnviosApi.Repositories;

public class ClienteRepository : IClienteRepository
{
     private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context) => _context = context;

    public Task<List<Cliente>> GetAllAsync() =>
        _context.Clientes.AsNoTracking().ToListAsync();

    public Task<Cliente?> GetByIdAsync(int id) =>
        _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> DniExistsAsync(string dni, int? excludeId = null) =>
        _context.Clientes.AnyAsync(c => c.Dni == dni && c.Id != excludeId);

    public Task<bool> HasEnviosAsync(int id) =>
        _context.Envios.AnyAsync(e => e.ClienteId == id);

    public async Task<Cliente> CreateAsync(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return cliente;
    }

    public async Task UpdateAsync(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Cliente cliente)
    {
        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();
    }

}
