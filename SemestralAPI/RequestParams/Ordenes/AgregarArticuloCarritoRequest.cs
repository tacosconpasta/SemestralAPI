using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Ordenes {
  public class AgregarArticuloCarritoRequest {
    public int OrdenId { get; set; }
    public int ArticuloId { get; set; }
    public int Cantidad { get; set; }
  }
}
