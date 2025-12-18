namespace SemestralAPI.Models {
  public class Factura {
    // Tanto el descuento como el cupón son opcionales, por eso se marcan como nullable
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
    public float Total { get; set; }
    public float Itbms { get; set; }
    public float Subtotal { get; set; }
    public float? Descuento { get; set; }
    public int? CuponId { get; set; }
  }
}
