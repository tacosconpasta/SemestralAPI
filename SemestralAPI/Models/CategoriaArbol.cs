namespace SemestralAPI.Models {
  public class CategoriaArbol {
    public int Id { get; set; }
    public string Nombre { get; set; }
    public List<CategoriaArbol> Hijos { get; set; } = new();
  }
}
