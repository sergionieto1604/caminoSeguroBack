using CaminoSeguro.Api.Data;
using CaminoSeguro.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaminoSeguro.Api.Controllers;

[ApiController]
[Route("api/municipios")]
public class MunicipiosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<MunicipioDto>> Listar(CancellationToken ct) =>
        await db.Municipios.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MunicipioDto { Id = m.Id, Nombre = m.Nombre, Latitud = m.Latitud, Longitud = m.Longitud })
            .ToListAsync(ct);
}

[ApiController]
[Route("api/categorias")]
public class CategoriasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<CategoriaDto>> Listar(CancellationToken ct) =>
        await db.Categorias.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, ColorHex = c.ColorHex })
            .ToListAsync(ct);
}
