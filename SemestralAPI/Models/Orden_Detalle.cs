namespace SemestralAPI.Models {
  public class Orden_Detalle {
    public required int Id { get; set; }
    public required int OrdenId { get; set; }
    public required int ArticuloId { get; set; }
    public required int Cantidad { get; set; }
    public required decimal PrecioUnitario { get; set; }
    public required decimal PrecioFinal { get; set; }
  }
}
