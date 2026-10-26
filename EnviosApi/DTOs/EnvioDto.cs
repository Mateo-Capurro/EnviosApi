using System;
using EnviosApi.Models;
namespace EnviosApi.DTOs;

public class EnvioDto
{
public int Id { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public EstadoEnvio Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int ClienteId { get; set; }
}
