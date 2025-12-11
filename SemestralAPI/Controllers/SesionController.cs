using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Sesion;
using SemestralAPI.Services;
using System.Text.Json;

namespace SemestralAPI.Controllers {
  [ApiController]
  [Route("api/auth")]
  public class SesionController : ControllerBase {
    private BaseDatos _bd = new BaseDatos(
      Environment.GetEnvironmentVariable("HOST_NAME")!,
      Environment.GetEnvironmentVariable("DB_NAME")!,
      Environment.GetEnvironmentVariable("DB_USER")!,
      Environment.GetEnvironmentVariable("DB_PASSWORD")!
     );

    /* Si se usuara AuthToken
    public SesionController(AuthService authService) {
      _authService = authService;
    }
    */

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request) {
      var usuario = _bd.IniciarSesion(request.User.Trim(), request.Contrasena.Trim());

      //Si no se econtró un usuario
      if (usuario == null) {
        return NotFound("No se encontró ningún usuario con esas credenciales.");
      }

      string json = JsonSerializer.Serialize(usuario);

      //Si se encontró un cliente
      if (usuario.GetType() == typeof(Cliente)) {
        return Ok(json);
      }

      //Si se encontró un administrador
      if (usuario.GetType() == typeof(Usuario)) {
        return Ok(json);
      }

      return NotFound("No se encontró ningún usuario con esas credenciales.");
    }

  }
}
