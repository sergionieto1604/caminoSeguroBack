namespace CaminoSeguro.Models
{
    public class Reporte
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int CategoriaId { get; set; }
        public int MunicipioId { get; set; }

        public string PuntoReferencia { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? FranjaHoraria { get; set; } 

        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string FuenteCoordenadas { get; set; } = string.Empty;

        public bool EsAnonimo { get; set; } 

        public int Confirmaciones { get; set; } = 0;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

      
        public Users? Usuario { get; set; }
        public Category? Categoria { get; set; }
        public Municipio? Municipio { get; set; }
    }
}
