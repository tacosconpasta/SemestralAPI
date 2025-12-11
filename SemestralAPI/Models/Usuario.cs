namespace SemestralAPI.Models {
  public class Usuario {
    // ClienteId es opcional, ya que no todos los usuarios son clientes
    public int Id { get; set; }
    public int? ClienteId { get; set; }
    public string User { get; set; }
    public string Contrasena { get; set; }
    public string Rol { get; set; }
  }
}