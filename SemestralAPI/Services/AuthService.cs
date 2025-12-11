using System.Collections.Generic;
using System.Linq;
using SemestralAPI.Models;

//esta clase está SUSPENDIDA
//Porque el approach con Tokens es eliminado por 
//restricciones de tiempo

namespace SemestralAPI.Services {
  public class AuthService {
    // Datos de ejemplo, prueba
    private List<Usuario> UsuariosFake = new List<Usuario>()
   {
    new Usuario {
        Id = 1,
        ClienteId = null,
        User = "admin",
        Contrasena = "1234",
        Rol = "admin"
    }
};


    // Nota: Usuario? permite retornar null si no encuentra
    public Usuario? ValidarCredenciales(string user, string password) {
      return UsuariosFake
          .FirstOrDefault(u => u.User == user && u.Contrasena == password);
    }
  }
}
