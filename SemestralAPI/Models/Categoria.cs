namespace SemestralAPI.Models
{
  // El campo CategoriaPadreId es opcional para permitir categorías raíz, por lo que es de tipo int?
  public class Categoria
    {
    public int Id { get; set; }
    public string Nombre { get; set; }
    public int? CategoriaPadreId { get; set; }
  }
}
