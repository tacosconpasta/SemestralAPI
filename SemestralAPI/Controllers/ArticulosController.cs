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
  //Automáticamente enruta todos los métodos de la clase "ArticulosController"
  //bajo una ruta central "Articulos"
  [Route("api/articulos")]

  // FIN DECORADORES //


  //Declarar Clase
  public class ArticulosController : Controller {

    private BaseDatos bd = new BaseDatos(
      Environment.GetEnvironmentVariable("HOST_NAME")!,
      Environment.GetEnvironmentVariable("DB_NAME")!,
      Environment.GetEnvironmentVariable("DB_USER")!,
      Environment.GetEnvironmentVariable("DB_PASSWORD")!
      );

    //Obtener Articulos
    //Al solicitar "GET" a la ruta "articulos"
    [HttpGet]
    public ActionResult<List<Articulo>> ObtenerArticulos() {
      return Ok("ok");
    }



    //Obtener Articulos por Id
    //Al Solicitar "GET" a la ruta "/articulos/producto_id"
    [HttpGet("{id}")]
    public ActionResult<Articulo> ObtenerProductoPorId(int id) {
      if (bd.ProbarConexion()) {
        return Ok("Se conectó a la BD bro");
      } else {
        return NotFound("No se conectó a NADA");
      }
    }


    //Insertar Producto
    //Al solicitar "POST" a la ruta "articulos"
    [HttpPost]
    public ActionResult<int> InsertarProducto(InsertarProductoRequest parametros) {
      return Ok("Producto Insertado");
    }


  }
}
