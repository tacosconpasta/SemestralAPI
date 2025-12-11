using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Articulos {
  public class InsertarArticuloRequest {

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Range(0.01, float.MaxValue)]
    public float Precio { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Required]
    public bool PagaITBMS { get; set; }
  }
}
