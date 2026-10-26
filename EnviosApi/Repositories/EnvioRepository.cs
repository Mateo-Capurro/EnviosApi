using System;
using EnviosApi.Data;
using EnviosApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosApi.Repositories;

public class EnvioRepository : IEnvioRepository
{
 private readonly AppDbContext _context;

    public EnvioRepository(AppDbContext context) => _context = context;

    public Task<List<Envio>> GetAllAsync() =>
        _context.Envios.AsNoTracking().ToListAsync();

    public Task<Envio?> GetByIdAsync(int id) =>
        _context.Envios.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Envio> CreateAsync(Envio envio)
    {
        _context.Envios.Add(envio);
        await _context.SaveChangesAsync();
        return envio;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var envio = await _context.Envios.FindAsync(id);
        if (envio is null) return false;
        _context.Envios.Remove(envio);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ClienteExistsAsync(int clienteId) =>
        _context.Clientes.AnyAsync(c => c.Id == clienteId);


        public async Task UpdateAsync(Envio envio)
{
    _context.Envios.Update(envio);
    await _context.SaveChangesAsync();
}
}

