using Microsoft.AspNetCore.Mvc;
using SemestralAPI.RequestParams.Sesion;
using SemestralAPI.Services;

namespace SemestralAPI.Controllers {
  [ApiController]
  [Route("api/auth")]
  public class SesionController : ControllerBase {
    private readonly AuthService _authService;

    public SesionController(AuthService authService) {
      _authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request) {
      var usuario = _authService.ValidarCredenciales(request.User, request.Contrasena);

      if (usuario == null)
        return Unauthorized(new { mensaje = "Credenciales incorrectas" });

      string token = Guid.NewGuid().ToString();

      return Ok(new LoginResponse {
        AuthToken = token
      });
    }

  }
}
