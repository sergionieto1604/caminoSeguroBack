using System.ComponentModel.DataAnnotations;
using CaminoSeguro.Api.Models;

namespace CaminoSeguro.Api.Dtos;

// Contrato con el frontend (src/app/core/models.ts). ASP.NET Core serializa en camelCase.

public class RegistroRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(254, ErrorMessage = "El correo es demasiado largo.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El alias es obligatorio.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "El alias debe tener entre 3 y 30 caracteres.")]
    public string Alias { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEn { get; set; } // UTC -> se serializa con "Z"
    public string Alias { get; set; } = "";
    public string Email { get; set; } = "";
}

public class MunicipioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public double Latitud { get; set; }
    public double Longitud { get; set; }
}

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string ColorHex { get; set; } = "";
}

public class ReporteRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Elige un municipio.")]
    public int MunicipioId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Elige una categoría.")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "El punto de referencia es obligatorio.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El punto de referencia debe tener entre 5 y 200 caracteres.")]
    public string PuntoReferencia { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede pasar de 500 caracteres.")]
    public string? Descripcion { get; set; }

    public string? FranjaHoraria { get; set; }

    public bool EsAnonimo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FranjaHoraria is not null && !FranjasHorarias.Validas.Contains(FranjaHoraria))
            yield return new ValidationResult("La franja horaria debe ser Tarde, Noche o Madrugada.",
                new[] { nameof(FranjaHoraria) });

        if (PuntoReferencia is not null && PuntoReferencia.Trim().Length < 5)
            yield return new ValidationResult("El punto de referencia debe tener entre 5 y 200 caracteres.",
                new[] { nameof(PuntoReferencia) });
    }
}

public class ReporteDto
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = "";
    public string ColorHex { get; set; } = "";
    public int MunicipioId { get; set; }
    public string Municipio { get; set; } = "";
    public string PuntoReferencia { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? FranjaHoraria { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string FuenteCoordenadas { get; set; } = "";
    /// <summary>Alias o "Anónima". NUNCA el correo.</summary>
    public string Autor { get; set; } = "";
    public int Confirmaciones { get; set; }
    public bool YaConfirme { get; set; }
    public bool EsMio { get; set; }
    public bool EsAnonimo { get; set; }
    public double? DistanciaKm { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
