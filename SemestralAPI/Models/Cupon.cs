namespace SemestralAPI.Models {
  public class Cupon {
    public required int Id { get; set; }
    public required string Codigo { get; set; }
    public required decimal Descuento { get; set; }
    public required bool Estado { get; set; }
  }
}
