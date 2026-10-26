using System;
using EnviosApi.DTOs;
using EnviosApi.Models;

namespace EnviosApi.Services;

public interface ITokenService
{
    TokenDto GenerarToken(Usuario usuario);
}
