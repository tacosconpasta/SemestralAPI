using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Articulos.Categorias {
  public class ActualizarCategoriaRequest {
    [StringLength(150)]
    public string? Nombre { get; set; }

    public int? CategoriaPadreId { get; set; }
  }
}
