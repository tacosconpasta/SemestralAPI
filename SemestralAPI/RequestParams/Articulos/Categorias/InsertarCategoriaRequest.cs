using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Articulos.Categorias {
  public class InsertarCategoriaRequest {
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    // Puede ser null si es categoría raíz
    public int? CategoriaPadreId { get; set; }
  }
}
