namespace SemestralAPI.RequestParams.Ordenes {
  public class EditarArticuloCarritoRequest {
    public int OrdenId { get; set; }
    public int ArticuloId { get; set; }
    public int Cantidad { get; set; }
  }
}
