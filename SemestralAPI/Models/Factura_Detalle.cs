namespace SemestralAPI.Models {
  public class Factura_Detalle {
    public required int Id { get; set; }
    public required int FacturaId { get; set; }
    public required int ArticuloId { get; set; }
    public required int Cantidad { get; set; }
    public required decimal PrecioUnitario { get; set; }
    public required decimal PrecioFinal { get; set; }
  }
}
