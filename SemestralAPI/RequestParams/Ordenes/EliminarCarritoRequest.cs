using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Ordenes {
  public class EliminarArticuloCarritoRequest {
    public int OrdenId { get; set; }
    public int ArticuloId { get; set; }
  }
}
