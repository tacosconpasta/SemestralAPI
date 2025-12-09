namespace SemestralAPI.Models {
  public class Usuario {
    // ClienteId es opcional, ya que no todos los usuarios son clientes
    public required int Id { get; set; }
    public required int? ClienteId { get; set; }
    public required string User { get; set; }
    public required string Contrasena { get; set; }
    public required string Rol { get; set; }
  }
}
