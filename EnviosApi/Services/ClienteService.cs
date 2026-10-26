using System;
using EnviosApi.DTOs;
using EnviosApi.Exceptions;
using EnviosApi.Models;
using EnviosApi.Repositories;
namespace EnviosApi.Services;

public class ClienteService : IClienteService

{
     private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository) => _repository = repository;

    public async Task<IEnumerable<ClienteDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto);

    public async Task<ClienteDto> GetByIdAsync(int id) =>
        ToDto(await ObtenerAsync(id));

    public async Task<ClienteDto> CreateAsync(ClienteCreateDto dto)
    {
        if (await _repository.DniExistsAsync(dto.Dni))
            throw new ConflictException($"Ya existe un cliente con el DNI {dto.Dni}.");

        var cliente = await _repository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Dni = dto.Dni,
            Email = dto.Email
        });
        return ToDto(cliente);
    }

    public async Task<ClienteDto> UpdateAsync(int id, ClienteCreateDto dto)
    {
        var cliente = await ObtenerAsync(id);

        if (await _repository.DniExistsAsync(dto.Dni, id))
            throw new ConflictException($"Ya existe otro cliente con el DNI {dto.Dni}.");

        cliente.Nombre = dto.Nombre;
        cliente.Dni = dto.Dni;
        cliente.Email = dto.Email;
        await _repository.UpdateAsync(cliente);
        return ToDto(cliente);
    }

    public async Task DeleteAsync(int id)
    {
        var cliente = await ObtenerAsync(id);

        if (await _repository.HasEnviosAsync(id))
            throw new ConflictException("No se puede eliminar un cliente que tiene envíos.");

        await _repository.DeleteAsync(cliente);
    }

    private async Task<Cliente> ObtenerAsync(int id) =>
        await _repository.GetByIdAsync(id)
        ?? throw new NotFoundException($"No existe el cliente con id {id}.");

    private static ClienteDto ToDto(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Dni = c.Dni,
        Email = c.Email
    }; 

}
