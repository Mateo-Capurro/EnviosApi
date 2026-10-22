using System;

namespace EnviosApi.Models;

public class Envio
{
  public int Id { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public EstadoEnvio Estado { get; set; } = EstadoEnvio.Pendiente;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}
