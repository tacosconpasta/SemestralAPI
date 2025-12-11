using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Productos;

namespace SemestralAPIClone.Controllers {

  // DECORADORES //

  //Define esta clase como un controlador de API para
  //ser identificada por ASP.NET automáticamente
  [ApiController]

  //[controller] = esta clase
  //Automáticamente enruta todos los métodos de la clase "ProductosController"
  //bajo una ruta central "Productos"
  [Route("api/[controller]")]

  // FIN DECORADORES //


  //Declarar Clase
  public class ProductosController : Controller {

    private BaseDatos bd = new BaseDatos(
      Environment.GetEnvironmentVariable("HOST_NAME")!,
      Environment.GetEnvironmentVariable("DB_NAME")!,
      Environment.GetEnvironmentVariable("DB_USER")!,
      Environment.GetEnvironmentVariable("DB_PASSWORD")!
      );

    //Obtener Productos
    //Al solicitar "GET" a la ruta "[controller]"
    [HttpGet]
    public ActionResult<List<Articulo>> ObtenerProductos() {
      if (!bd.ProbarConexion())
        return StatusCode(500, "Error: No se pudo conectar a la base de datos.");

      string query = "SELECT id, nombre, descripcion, precio, stock, paga_itbms FROM articulo;";

      List<Articulo> lista = bd.LeerTabla<Articulo>(query);

      return Ok(new {
        producto = lista
      });
    }

    // 📌 Obtener UN producto por Id  →  GET /api/productos/{id}
        // ===============================================================
        [HttpGet("{id}")]
    public ActionResult<Articulo> ObtenerProductoPorId(int id) {
      if (!bd.ProbarConexion())
        return StatusCode(500, "Error: No se pudo conectar a la base de datos.");

      string query =
          $"SELECT id, nombre, descripcion, precio, stock, paga_itbms, created_at, updated_at FROM articulo WHERE id = {id};";

      List<Articulo> lista = bd.LeerTabla<Articulo>(query);

      if (lista.Count == 0)
        return NotFound(new { message = $"Producto con id {id} no encontrado" });

      return Ok(new {
        id = id,
        producto = lista.First()
      });
    }

    //

    //Insertar Producto
    //Al solicitar "POST" a la ruta "[controller]"
    [HttpPost]
    public ActionResult<int> InsertarProducto(InsertarProductoRequest parametros) {
      return Ok("Producto Insertado");
    }


  }
}
