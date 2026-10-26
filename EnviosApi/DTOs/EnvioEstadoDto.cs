using System.ComponentModel.DataAnnotations;
using EnviosApi.Models;

namespace EnviosApi.DTOs;

public class EnvioEstadoDto
{
    [EnumDataType(typeof(EstadoEnvio))]
    public EstadoEnvio Estado { get; set; }
}