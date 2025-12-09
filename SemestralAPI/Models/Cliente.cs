namespace SemestralAPI.Models {
  // Modelo de datos para representar un cliente
  public class Cliente {
    public required int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Telefono { get; set; }
    public required string Correo { get; set; }
    public required string Direccion { get; set; }
  }
}
