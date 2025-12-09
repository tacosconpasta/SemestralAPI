namespace SemestralAPI.Models {
  public class Factura {
    // Tanto el descuento como el cupón son opcionales, por eso se marcan como nullable
    public required int Id { get; set; }
    public required int UsuarioId { get; set; }
    public required DateTime Fecha { get; set; }
    public required decimal Total { get; set; }
    public required decimal Itbms { get; set; }
    public required decimal Subtotal { get; set; }
    public decimal? Descuento { get; set; }
    public int? CuponId { get; set; }
  }
}
