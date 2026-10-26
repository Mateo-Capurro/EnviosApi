using System;
using EnviosApi.Data;
using EnviosApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosApi.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context) => _context = context;

    public Task<Usuario?> GetByEmailAsync(string email) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailExistsAsync(string email) =>
        _context.Usuarios.AnyAsync(u => u.Email == email);

    public Task<bool> AnyAsync() =>
        _context.Usuarios.AnyAsync();

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
}
