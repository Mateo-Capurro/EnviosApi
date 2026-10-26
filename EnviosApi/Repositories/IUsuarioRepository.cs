using System;
using EnviosApi.Models;

namespace EnviosApi.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> AnyAsync();
    Task<Usuario> CreateAsync(Usuario usuario);
}
