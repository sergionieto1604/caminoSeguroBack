using System.Globalization;
using System.Text.Json.Serialization;
using CaminoSeguro.Api.Models;

namespace CaminoSeguro.Api.Services;

public interface IGeocodingService
{
    /// <summary>Devuelve las coordenadas del punto de referencia, o null si no se encuentra.</summary>
    Task<(double Lat, double Lng)?> BuscarAsync(string puntoReferencia, Municipio municipio, CancellationToken ct);
}

/// <summary>
/// Geocodificación con Nominatim (OpenStreetMap).
/// Si Nominatim falla, tarda o devuelve un punto a más de 25 km del municipio,
/// se usa el centro del municipio (fuenteCoordenadas = 'CentroMunicipio').
/// </summary>
public class NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> log) : IGeocodingService
{
    private const double DistanciaMaximaKm = 25;

    public async Task<(double Lat, double Lng)?> BuscarAsync(string puntoReferencia, Municipio municipio, CancellationToken ct)
    {
        try
        {
            var q = Uri.EscapeDataString($"{puntoReferencia}, {municipio.Nombre}, Cundinamarca, Colombia");
            var url = $"search?format=jsonv2&limit=1&countrycodes=co&q={q}";

            var resultados = await http.GetFromJsonAsync<List<ResultadoNominatim>>(url, ct);
            var r = resultados?.FirstOrDefault();
            if (r is null) return null;

            if (!double.TryParse(r.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(r.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                return null;

            // Evita aceptar un homónimo en otra parte del país.
            if (Geo.DistanciaKm(lat, lng, municipio.Latitud, municipio.Longitud) > DistanciaMaximaKm)
                return null;

            return (lat, lng);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Nominatim no respondió; se usa el centro del municipio.");
            return null;
        }
    }

    private sealed class ResultadoNominatim
    {
        [JsonPropertyName("lat")] public string Lat { get; set; } = "";
        [JsonPropertyName("lon")] public string Lon { get; set; } = "";
    }
}
