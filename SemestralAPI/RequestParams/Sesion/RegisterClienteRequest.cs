namespace SemestralAPI.RequestParams.Sesion {
  public class RegisterClienteRequest {
    //Datos relacionados a cliente
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Telefono { get; set; }
    public string Correo { get; set; }
    public string Direccion { get; set; }

    //Datos relacionados a Usuario
    public string User { get; set; }
    public string Contrasena { get; set; }
  }
}
