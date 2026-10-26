using System;
using System.ComponentModel.DataAnnotations;

namespace EnviosApi.DTOs;

public class RegisterDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
