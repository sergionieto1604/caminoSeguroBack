namespace CaminoSeguro.Models
{
    public class Municipio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
    }
}
