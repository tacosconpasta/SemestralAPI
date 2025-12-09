namespace SemestralAPI.Models {
  public class Orden {
    // Mismo caso que en Factura.cs, Descuento y CuponId son opcionales
    public required int Id { get; set; }
    public required int UsuarioId { get; set; }
    public required string Estado { get; set; }
    public required DateTime Fecha { get; set; }
    public required decimal Total { get; set; }
    public required decimal Itbms { get; set; }
    public required decimal Subtotal { get; set; }
    public decimal? Descuento { get; set; }
    public int? CuponId { get; set; }
  }
}
