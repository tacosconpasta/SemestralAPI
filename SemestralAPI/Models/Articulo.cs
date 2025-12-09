namespace SemestralAPI.Models
{
    public class Articulo
    {
    // Propiedades de artículo
        public required int Id { get; set; }
        public required string Nombre { get; set; }
        public required decimal Precio { get; set; }
        public required int Stock { get; set; }
        public required bool Paga_itbms { get; set; }
    }
}
