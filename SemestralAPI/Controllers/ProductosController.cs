using Microsoft.AspNetCore.Mvc;
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

    //productosEjemplo, esto sería reemplazado por conexión a BD en cada endpoint.
    List<Articulo> productosBaseDatos { get; set; } = new List<Articulo>();


    //Obtener Productos
    //Al solicitar "GET" a la ruta "[controller]"
    [HttpGet]
    public ActionResult<List<Articulo>> ObtenerProductos() {
      return Ok(productosBaseDatos);
    }



    //Obtener Productos por Id
    //Al Solicitar "GET" a la ruta "/[controller]/producto_id"
    [HttpGet("{id}")]
    public ActionResult<Articulo> ObtenerProductoPorId(int id) {

      //Buscar producto por Id
      foreach (Articulo producto in productosBaseDatos)
        if (producto.id == id) return Ok(producto);

      return NotFound("No se encontró ese producto");
    }


    //Insertar Producto
    //Al solicitar "POST" a la ruta "[controller]"
    [HttpPost]
    public ActionResult<int> InsertarProducto(InsertarProductoRequest parametros) {
      return Ok("Producto Insertado");
    }


  }
}
