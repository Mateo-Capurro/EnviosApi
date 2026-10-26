using System;
using System.ComponentModel.DataAnnotations;
namespace EnviosApi.DTOs;

public class EnvioCreateDto
{

    [Required, StringLength(200)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    public int ClienteId { get; set; }
}
