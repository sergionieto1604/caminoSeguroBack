using CaminoSeguro.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CaminoSeguro.Api.Data;

/// <summary>Datos fijos: municipios de Cundinamarca (centro aproximado) y categorías de riesgo.</summary>
public static class Semilla
{
    public static void Cargar(ModelBuilder b)
    {
        b.Entity<Municipio>().HasData(
            new Municipio { Id = 1, Nombre = "Bogotá D.C.", Latitud = 4.7110, Longitud = -74.0721 },
            new Municipio { Id = 2, Nombre = "Soacha", Latitud = 4.5794, Longitud = -74.2168 },
            new Municipio { Id = 3, Nombre = "Fusagasugá", Latitud = 4.3365, Longitud = -74.3638 },
            new Municipio { Id = 4, Nombre = "Facatativá", Latitud = 4.8136, Longitud = -74.3545 },
            new Municipio { Id = 5, Nombre = "Zipaquirá", Latitud = 5.0221, Longitud = -74.0048 },
            new Municipio { Id = 6, Nombre = "Chía", Latitud = 4.8584, Longitud = -74.0589 },
            new Municipio { Id = 7, Nombre = "Mosquera", Latitud = 4.7059, Longitud = -74.2302 },
            new Municipio { Id = 8, Nombre = "Madrid", Latitud = 4.7325, Longitud = -74.2642 },
            new Municipio { Id = 9, Nombre = "Funza", Latitud = 4.7163, Longitud = -74.2117 },
            new Municipio { Id = 10, Nombre = "Girardot", Latitud = 4.3032, Longitud = -74.8030 },
            new Municipio { Id = 11, Nombre = "Cajicá", Latitud = 4.9186, Longitud = -74.0280 },
            new Municipio { Id = 12, Nombre = "Sibaté", Latitud = 4.4907, Longitud = -74.2596 },
            new Municipio { Id = 13, Nombre = "La Calera", Latitud = 4.7206, Longitud = -73.9680 },
            new Municipio { Id = 14, Nombre = "Villeta", Latitud = 5.0128, Longitud = -74.4729 },
            new Municipio { Id = 15, Nombre = "Ubaté", Latitud = 5.3072, Longitud = -73.8143 },
            new Municipio { Id = 16, Nombre = "Tocancipá", Latitud = 4.9650, Longitud = -73.9130 },
            new Municipio { Id = 17, Nombre = "Silvania", Latitud = 4.4036, Longitud = -74.3880 },
            new Municipio { Id = 18, Nombre = "La Mesa", Latitud = 4.6305, Longitud = -74.4625 }
        );

        b.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Poca iluminación", ColorHex = "#F59E0B" },
            new Categoria { Id = 2, Nombre = "Zona solitaria", ColorHex = "#64748B" },
            new Categoria { Id = 3, Nombre = "Acoso callejero", ColorHex = "#DC2626" },
            new Categoria { Id = 4, Nombre = "Robo o atraco", ColorHex = "#7C3AED" },
            new Categoria { Id = 5, Nombre = "Transporte inseguro", ColorHex = "#0EA5E9" },
            new Categoria { Id = 6, Nombre = "Otro", ColorHex = "#0F766E" }
        );
    }
}
