namespace SemestralAPI.Models {
  public class Foto {
    // FotoExtra2, FotoExtra3 y FotoExtra4 son opcionales igual que en la base de datos
    public required int Id { get; set; }
    public required int ArticuloId { get; set; }
    public required string FotoPrincipal { get; set; }
    public string? FotoExtra2 { get; set; }
    public string? FotoExtra3 { get; set; }
    public string? FotoExtra4 { get; set; }
  }
}
