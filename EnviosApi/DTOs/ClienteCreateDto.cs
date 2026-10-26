using System;
using System.ComponentModel.DataAnnotations;

namespace EnviosApi.DTOs;

public class ClienteCreateDto
{
 [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos.")]
    public string Dni { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}
