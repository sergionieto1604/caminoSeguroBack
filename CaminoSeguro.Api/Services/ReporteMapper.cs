using CaminoSeguro.Api.Dtos;
using CaminoSeguro.Api.Models;

namespace CaminoSeguro.Api.Services;

public static class ReporteMapper
{
    /// <summary>
    /// Convierte la entidad (con Categoria, Municipio, Usuario y Confirmaciones cargados) al DTO del front.
    /// </summary>
    public static ReporteDto ADto(Reporte r, int? usuarioActualId, (double Lat, double Lng)? referencia)
    {
        return new ReporteDto
        {
            Id = r.Id,
            CategoriaId = r.CategoriaId,
            Categoria = r.Categoria.Nombre,
            ColorHex = r.Categoria.ColorHex,
            MunicipioId = r.MunicipioId,
            Municipio = r.Municipio.Nombre,
            PuntoReferencia = r.PuntoReferencia,
            Descripcion = r.Descripcion,
            FranjaHoraria = r.FranjaHoraria,
            Latitud = r.Latitud,
            Longitud = r.Longitud,
            FuenteCoordenadas = r.FuenteCoordenadas,
            Autor = r.EsAnonimo ? "Anónima" : r.Usuario.Alias,
            Confirmaciones = r.Confirmaciones.Count,
            YaConfirme = usuarioActualId is not null && r.Confirmaciones.Any(c => c.UsuarioId == usuarioActualId),
            EsMio = usuarioActualId is not null && r.UsuarioId == usuarioActualId,
            EsAnonimo = r.EsAnonimo,
            DistanciaKm = referencia is null
                ? null
                : Math.Round(Geo.DistanciaKm(referencia.Value.Lat, referencia.Value.Lng, r.Latitud, r.Longitud), 2),
            FechaCreacion = r.FechaCreacion
        };
    }
}
