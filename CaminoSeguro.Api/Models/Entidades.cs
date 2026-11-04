namespace CaminoSeguro.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Alias { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;

    public List<Reporte> Reportes { get; set; } = new();
}

public class Municipio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public double Latitud { get; set; }
    public double Longitud { get; set; }
}

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string ColorHex { get; set; } = "#6B7280";
}

public class Reporte
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int MunicipioId { get; set; }
    public Municipio Municipio { get; set; } = null!;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public string PuntoReferencia { get; set; } = "";
    public string? Descripcion { get; set; }
    /// <summary>'Tarde' | 'Noche' | 'Madrugada' | null</summary>
    public string? FranjaHoraria { get; set; }

    public double Latitud { get; set; }
    public double Longitud { get; set; }
    /// <summary>'Nominatim' | 'CentroMunicipio'</summary>
    public string FuenteCoordenadas { get; set; } = FuentesCoordenadas.CentroMunicipio;

    public bool EsAnonimo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public List<Confirmacion> Confirmaciones { get; set; } = new();
}

/// <summary>"Yo también lo he visto": una usuaria confirma el reporte de otra.</summary>
public class Confirmacion
{
    public int ReporteId { get; set; }
    public Reporte Reporte { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}

public static class FuentesCoordenadas
{
    public const string Nominatim = "Nominatim";
    public const string CentroMunicipio = "CentroMunicipio";
}

public static class FranjasHorarias
{
    public static readonly string[] Validas = { "Tarde", "Noche", "Madrugada" };
}
