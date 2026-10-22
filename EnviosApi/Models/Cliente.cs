using System;

namespace EnviosApi.Models;

public class Cliente
{
     public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Envio> Envios { get; set; } = new();

}
