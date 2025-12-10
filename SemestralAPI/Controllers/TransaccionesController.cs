using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;

namespace SemestralAPI.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class TransaccionesController : Controller {
    private readonly BaseDatos _db;

    public TransaccionesController() {
      _db = new BaseDatos(
          Environment.GetEnvironmentVariable("HOST_NAME"),
          Environment.GetEnvironmentVariable("DB_NAME"),
          Environment.GetEnvironmentVariable("DB_USER"),
          Environment.GetEnvironmentVariable("DB_PASSWORD")
      );
    }

    [HttpGet("{id}")]
    public ActionResult ConsultarTransaccion(int id) {
      // Obtener orden principal
      var orden = _db.Consultar(
          "SELECT * FROM orden WHERE id = @id",
          new Dictionary<string, object> { { "@id", id } }
      );

      if (orden.Count == 0)
        return NotFound(new { message = "La transacción no existe" });

      // Obtener detalles
      var detalles = _db.Consultar(
          "SELECT * FROM orden_detalle WHERE orden_id = @id",
          new Dictionary<string, object> { { "@id", id } }
      );

      return Ok(new {
        transaccion = new {
          orden = orden[0],
          detalles = detalles
        }
      });
    }
  }
}
