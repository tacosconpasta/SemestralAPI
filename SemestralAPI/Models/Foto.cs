namespace SemestralAPI.Models {
  public class Foto {
    // FotoExtra2, FotoExtra3 y FotoExtra4 son opcionales igual que en la base de datos
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public byte FotoPrincipal { get; set; }
  }
}
