using System.IdentityModel.Tokens.Jwt;
using CaminoSeguro.Api.Data;
using CaminoSeguro.Api.Dtos;
using CaminoSeguro.Api.Models;
using CaminoSeguro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CaminoSeguro.Api.Controllers;

[ApiController]
[Route("api/reportes")]
public class ReportesController(AppDbContext db, IGeocodingService geocoding) : ControllerBase
{
    private const int MaxResultados = 500;

    /// <summary>Id de la usuaria autenticada, o null si no vino token (o vino inválido).</summary>
    private int? UsuarioId =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    private IQueryable<Reporte> Consulta() =>
        db.Reportes.AsNoTracking()
            .Include(r => r.Categoria)
            .Include(r => r.Municipio)
            .Include(r => r.Usuario)
            .Include(r => r.Confirmaciones)
            .AsSplitQuery();

    // GET /api/reportes?lat=&lng=  |  ?municipioRefId=   (&categoriaId= opcional)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReporteDto>>> Listar(
        [FromQuery] double? lat, [FromQuery] double? lng,
        [FromQuery] int? municipioRefId, [FromQuery] int? categoriaId,
        CancellationToken ct)
    {
        (double Lat, double Lng)? referencia = null;

        if (lat is not null && lng is not null)
        {
            if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
                return BadRequest(new { message = "Las coordenadas no son válidas." });
            referencia = (lat.Value, lng.Value);
        }
        else if (municipioRefId is not null)
        {
            var m = await db.Municipios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == municipioRefId, ct);
            if (m is null) return BadRequest(new { message = "El municipio de referencia no existe." });
            referencia = (m.Latitud, m.Longitud);
        }

        var q = Consulta();
        if (categoriaId is not null) q = q.Where(r => r.CategoriaId == categoriaId);

        var lista = await q.OrderByDescending(r => r.FechaCreacion).Take(MaxResultados).ToListAsync(ct);

        var uid = UsuarioId;
        var dtos = lista.Select(r => ReporteMapper.ADto(r, uid, referencia)).ToList();

        // Orden por distancia ascendente; en empate quedan primero los más recientes (OrderBy es estable).
        if (referencia is not null) dtos = dtos.OrderBy(d => d.DistanciaKm).ToList();

        return dtos;
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReporteDto>> Obtener(int id, CancellationToken ct)
    {
        var r = await Consulta().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound(new { message = "El reporte no existe." });
        return ReporteMapper.ADto(r, UsuarioId, null);
    }

    [HttpGet("mios")]
    [Authorize]
    public async Task<ActionResult<List<ReporteDto>>> Mios(CancellationToken ct)
    {
        var uid = UsuarioId!.Value;
        var lista = await Consulta()
            .Where(r => r.UsuarioId == uid)
            .OrderByDescending(r => r.FechaCreacion)
            .ToListAsync(ct);
        return lista.Select(r => ReporteMapper.ADto(r, uid, null)).ToList();
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("escritura")]
    public async Task<ActionResult<ReporteDto>> Crear(ReporteRequest req, CancellationToken ct)
    {
        var uid = UsuarioId!.Value;

        var (error, municipio) = await ValidarReferenciasAsync(req, ct);
        if (error is not null) return error;

        var reporte = new Reporte { UsuarioId = uid, FechaCreacion = DateTimeOffset.UtcNow };
        CopiarDatos(req, reporte);
        await AsignarCoordenadasAsync(reporte, municipio!, ct);

        db.Reportes.Add(reporte);
        await db.SaveChangesAsync(ct);

        var creado = await Consulta().FirstAsync(x => x.Id == reporte.Id, ct);
        return CreatedAtAction(nameof(Obtener), new { id = reporte.Id }, ReporteMapper.ADto(creado, uid, null));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [EnableRateLimiting("escritura")]
    public async Task<ActionResult<ReporteDto>> Editar(int id, ReporteRequest req, CancellationToken ct)
    {
        var uid = UsuarioId!.Value;

        var reporte = await db.Reportes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (reporte is null) return NotFound(new { message = "El reporte no existe." });
        if (reporte.UsuarioId != uid)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo la autora puede editar este reporte." });

        var (error, municipio) = await ValidarReferenciasAsync(req, ct);
        if (error is not null) return error;

        var cambioUbicacion = reporte.MunicipioId != req.MunicipioId ||
                              !string.Equals(reporte.PuntoReferencia, req.PuntoReferencia.Trim(), StringComparison.OrdinalIgnoreCase);

        CopiarDatos(req, reporte);
        if (cambioUbicacion) await AsignarCoordenadasAsync(reporte, municipio!, ct);

        await db.SaveChangesAsync(ct);

        var editado = await Consulta().FirstAsync(x => x.Id == id, ct);
        return ReporteMapper.ADto(editado, uid, null);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var uid = UsuarioId!.Value;

        var reporte = await db.Reportes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (reporte is null) return NotFound(new { message = "El reporte no existe." });
        if (reporte.UsuarioId != uid)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo la autora puede borrar este reporte." });

        db.Reportes.Remove(reporte); // las confirmaciones se borran en cascada
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/confirmaciones")]
    [Authorize]
    [EnableRateLimiting("escritura")]
    public async Task<IActionResult> Confirmar(int id, CancellationToken ct)
    {
        var uid = UsuarioId!.Value;

        var autoraId = await db.Reportes.Where(r => r.Id == id).Select(r => (int?)r.UsuarioId).FirstOrDefaultAsync(ct);
        if (autoraId is null) return NotFound(new { message = "El reporte no existe." });
        if (autoraId == uid) return BadRequest(new { message = "No puedes confirmar tu propio reporte." });

        var yaExiste = await db.Confirmaciones.AnyAsync(c => c.ReporteId == id && c.UsuarioId == uid, ct);
        if (!yaExiste)
        {
            db.Confirmaciones.Add(new Confirmacion { ReporteId = id, UsuarioId = uid });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* doble clic: ya quedó confirmada */ }
        }
        return NoContent();
    }

    [HttpDelete("{id:int}/confirmaciones")]
    [Authorize]
    public async Task<IActionResult> QuitarConfirmacion(int id, CancellationToken ct)
    {
        var uid = UsuarioId!.Value;
        await db.Confirmaciones.Where(c => c.ReporteId == id && c.UsuarioId == uid).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    // ---------- auxiliares ----------

    private async Task<(ActionResult? Error, Municipio? Municipio)> ValidarReferenciasAsync(ReporteRequest req, CancellationToken ct)
    {
        var municipio = await db.Municipios.AsNoTracking().FirstOrDefaultAsync(m => m.Id == req.MunicipioId, ct);
        if (municipio is null) return (BadRequest(new { message = "El municipio no existe." }), null);

        if (!await db.Categorias.AnyAsync(c => c.Id == req.CategoriaId, ct))
            return (BadRequest(new { message = "La categoría no existe." }), null);

        return (null, municipio);
    }

    private static void CopiarDatos(ReporteRequest req, Reporte r)
    {
        r.MunicipioId = req.MunicipioId;
        r.CategoriaId = req.CategoriaId;
        r.PuntoReferencia = req.PuntoReferencia.Trim();
        r.Descripcion = string.IsNullOrWhiteSpace(req.Descripcion) ? null : req.Descripcion.Trim();
        r.FranjaHoraria = req.FranjaHoraria;
        r.EsAnonimo = req.EsAnonimo;
    }

    private async Task AsignarCoordenadasAsync(Reporte r, Municipio m, CancellationToken ct)
    {
        var punto = await geocoding.BuscarAsync(r.PuntoReferencia, m, ct);
        if (punto is not null)
        {
            r.Latitud = punto.Value.Lat;
            r.Longitud = punto.Value.Lng;
            r.FuenteCoordenadas = FuentesCoordenadas.Nominatim;
        }
        else
        {
            r.Latitud = m.Latitud;
            r.Longitud = m.Longitud;
            r.FuenteCoordenadas = FuentesCoordenadas.CentroMunicipio;
        }
    }
}
