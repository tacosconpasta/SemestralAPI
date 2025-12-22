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

    //Obtener todas las ordenes en proceso
    [HttpGet("procesando")]
    public ActionResult ObtenerOrdenesEnProceso() {
      try {
        List<Orden> ordenes = bd.ObtenerOrdenesEnProceso();

        if (ordenes == null || ordenes.Count == 0)
          return NotFound(new { mensaje = "No hay órdenes en proceso." });

        return Ok(ordenes);

      } catch (Exception ex) {
        Console.WriteLine("Error en endpoint ObtenerOrdenesEnProceso: " + ex.Message);
        return StatusCode(500, "Error al obtener órdenes en proceso.");
      }
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

    //Actualizar Orden (solo estado)
    [HttpPut("{ordenId:int}")]
    public ActionResult ActualizarOrden(int ordenId, [FromBody] EditarOrdenRequest req) {

      //Validar id
      if (ordenId <= 0)
        return BadRequest("Id de orden inválido.");

      //Validar estado
      if (string.IsNullOrWhiteSpace(req.Estado))
        return BadRequest("Debe enviar un estado válido.");

      //Buscar orden
      Orden orden = bd.ObtenerOrdenPorId(ordenId);

      if (orden == null)
        return NotFound("La orden no existe.");

      //Actualizar estado
      orden.Estado = req.Estado;

      //Guardar cambios
      bool actualizado = bd.ActualizarEstadoOrden(ordenId, orden.Estado);

      if (!actualizado)
        return StatusCode(500, "No se pudo actualizar la orden.");

      //Volver a consultar para renderizar
      Orden ordenActualizada = bd.ObtenerOrdenPorId(ordenId);

      return Ok(ordenActualizada);
    }


    //Finalizar una orden (Checkout)
    [HttpPut("finalizar/{orden_id:int}")]
    public ActionResult FinalizarOrden(int orden_id) {

      if (orden_id <= 0)
        return BadRequest(new { mensaje = "El id de la orden debe ser válido." });

      Orden orden = bd.ObtenerOrdenPorId(orden_id);
      if (orden == null)
        return NotFound(new { mensaje = "La orden no existe." });

      try {
        bool finalizada = bd.FinalizarOrden(orden_id);

        if (!finalizada)
          return StatusCode(500, "No se pudo finalizar la orden.");

        return Ok(new {
          mensaje = "Orden finalizada y factura creada correctamente.",
          ordenId = orden_id,
          nuevoEstado = "revision"
        });

      } catch (InvalidOperationException ex) {
        if (ex.Message == "ESTADO_INVALIDO") {
          return BadRequest(new {
            mensaje = "La orden no se puede finalizar porque no está en proceso.",
            estadoActual = orden.Estado
          });
        }

        return StatusCode(500, "Error al finalizar la orden.");
      }
    }

  }
}
