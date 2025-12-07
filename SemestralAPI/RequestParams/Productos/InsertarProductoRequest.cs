using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Productos {
  public class InsertarProductoRequest {
    [Required]
    public string nombre { get; set; }
    public string descripcion { get; set; }
  }
}
