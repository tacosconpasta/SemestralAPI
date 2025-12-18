using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;

namespace SemestralAPI.Controllers {
  [ApiController]
  [Route("api/facturas")]
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

    [HttpGet]
    public IActionResult ObtenerFacturas() {
      var lista = _db.ObtenerFacturas();

      if (lista == null || lista.Count == 0)
        return NotFound(new { mensaje = "No hay facturas registradas." });

      return Ok(lista);
    }
  }
}

