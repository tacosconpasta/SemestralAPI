namespace SemestralAPI.RequestParams.Productos {
  public class EditarProductoRequest {
    public required string AuthToken { get; set; }
    public required SemestralAPI.Models.Articulo Producto { get; set; }
  }
}
