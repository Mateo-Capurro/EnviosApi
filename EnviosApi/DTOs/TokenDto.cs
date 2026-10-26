using System;

namespace EnviosApi.DTOs;

public class TokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
}
