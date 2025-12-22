using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
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

    //Especificar que el LoginRequest es de nuestro paquete y no del de Microsoft
    [HttpPost("login")]
    public ActionResult Login([FromBody] RequestParams.Sesion.LoginRequest request) {

      //Validar credenciales
      if (request == null || string.IsNullOrWhiteSpace(request.User) || string.IsNullOrWhiteSpace(request.Contrasena)) {
          return BadRequest("Credenciales inválidas.");
      }

      //Iniciar sesión
      var resultado = _bd.IniciarSesion(
        request.User.Trim(),
        request.Contrasena.Trim()
      );

      //Si no se econtró un usuario
      if (resultado == null) {
        return NotFound("No se encontró ningún usuario con esas credenciales.");
      }

      //Si se encontró un cliente
      if (resultado is Cliente cliente) {
        //Buscar información de usuario asociada a este cliente
        Usuario usuarioCliente = _bd.BuscarUsuarioByClienteId(cliente.Id);

        //Si no se econtró un cliente
        if (usuarioCliente == null) {
          return StatusCode(500, "El cliente no tiene usuario asociado.");
        }

        //Devolver informacion de cliente y usuario
        return Ok(new {
          usuario = new {
            id = usuarioCliente.Id,
            user = usuarioCliente.User,
            rol = usuarioCliente.Rol
          },
          cliente
        });
      }

      //Si se encontró un administrador, retornar sólo información de usuario
      if (resultado is Usuario admin) {
        return Ok( new {
          usuario = new {
            id = admin.Id,
            user = admin.User,
            rol = admin.Rol
          }
        });
      }

      return StatusCode(500, "No se pudo iniciar la sesión del usuario.");
    }


    [HttpPost("register/cliente")]
    public ActionResult<Cliente> Register([FromBody] RegisterClienteRequest request) {
      //Asignar información de usuario del cliente en request, a objeto de clase Usuario
      Usuario informacionUsuario = new Usuario { 
        User=request.User, 
        Contrasena=request.Contrasena, 
        Rol="cliente" };

      //Asignar información de cliente en request, a objeto de clase Cliente
      Cliente informacionCliente = new Cliente {
        Nombre = request.Nombre,
        Apellido = request.Apellido,
        Direccion = request.Direccion,
        Correo = request.Correo,
        Telefono = request.Telefono
      };

      bool esRegistradoCorrectamente = _bd.RegistrarCliente(informacionUsuario, informacionCliente);

      if (!esRegistradoCorrectamente) {
        return Conflict("Un usuario con el correo electrónico, o nombre de usuario, proveidos ya está registrado.");
      }

      return Accepted("Se ha registado correctamente.");
    }

    [HttpGet("clientes/{id}")]
    public ActionResult<Cliente> ObtenerClientePorId(int id) {
      var cliente = _bd.BuscarCliente(id);

      if (cliente == null)
        return NotFound(new { mensaje = "No se encontró el cliente con ese ID." });

      return Ok(cliente);
    }
  }
}
 