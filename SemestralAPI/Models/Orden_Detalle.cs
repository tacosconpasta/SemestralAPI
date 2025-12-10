namespace SemestralAPI.Models {
  public class Orden_Detalle {
    public int Id { get; set; }
    public int Orden_Id { get; set; }
    public int Articulo_Id { get; set; }
    public int Cantidad { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
    public decimal Precio_Unitario { get; set; }
    public decimal Precio_Final { get; set; }
  }
}
