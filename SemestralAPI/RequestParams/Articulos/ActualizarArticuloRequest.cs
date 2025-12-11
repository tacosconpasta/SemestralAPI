using System.ComponentModel.DataAnnotations;

namespace SemestralAPI.RequestParams.Articulos {
  public class ActualizarArticuloRequest {

    [StringLength(150)]
    public string? Nombre { get; set; }

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(0.01, float.MaxValue)]
    public float? Precio { get; set; }

    [Range(0, int.MaxValue)]
    public int? Stock { get; set; }

    public bool? PagaITBMS { get; set; }
  }
}
