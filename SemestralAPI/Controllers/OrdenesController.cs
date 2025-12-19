using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Ordenes;

namespace SemestralAPI.Controllers {

  [ApiController]
  [Route("api/ordenes")]
  public class OrdenesController : ControllerBase {

    private readonly BaseDatos bd;

    public OrdenesController() {
      bd = new BaseDatos(
        Environment.GetEnvironmentVariable("HOST_NAME")!,
        Environment.GetEnvironmentVariable("DB_NAME")!,
        Environment.GetEnvironmentVariable("DB_USER")!,
        Environment.GetEnvironmentVariable("DB_PASSWORD")!
      );
    }

    //Obtener información de orden en proceso (total de un carrito, subtotal del carrito...)
    [HttpGet("{usuarioId:int}")]
    public ActionResult ObtenerCarritoInfo(int usuarioId) {

      if (usuarioId <= 0)
        return BadRequest("Enviar un usuario válido.");

      Orden orden = bd.ObtenerOrdenEnProceso(usuarioId);

      if (orden == null)
        return NotFound("El usuario no tiene una orden en proceso.");

      return Ok(orden);
    }

    //Crear carrito (orden en proceso)
    [HttpPost("{usuarioId:int}")]
    public ActionResult CrearCarrito(int usuarioId) {

      if (usuarioId <= 0)
        return BadRequest("Enviar un usuario válido.");

      // Verificar si ya existe una orden en proceso
      Orden ordenExistente = bd.ObtenerOrdenEnProceso(usuarioId);

      if (ordenExistente != null)
        return Conflict("El usuario ya tiene una orden en proceso.");

      // Crear orden
      int ordenId = bd.CrearOrden(usuarioId);

      if (ordenId <= 0)
        return StatusCode(500, "No se pudo crear la orden.");

      return Ok(new {
        message = "Orden creada exitosamente.",
        orden_id = ordenId
      });
    }
  }
}
