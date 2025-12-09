namespace SemestralAPI.Models {
  // Representa la relación entre artículos y categorías
  public class Articulo_Categoria {
    public required int Id { get; set; }
    public required int ArticuloId { get; set; }
    public required int CategoriaId { get; set; }
  }
}
