namespace SemestralAPI.Models
{
    public class Articulo
    {
    // Propiedades de artículo
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public float Precio { get; set; }
        public int Stock { get; set; }
        public bool Paga_itbms { get; set; }
    }
}
