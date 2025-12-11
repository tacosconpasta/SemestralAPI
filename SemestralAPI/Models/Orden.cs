namespace SemestralAPI.Models {
  public class Orden {
    public int Id { get; set; }
    public string Estado { get; set; }
    public DateTime Fecha { get; set; }
    public int Usuario_Id { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
    public int? Cupon_Id { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal? Descuento { get; set; }
    public decimal Itbms { get; set; }

  }
}

