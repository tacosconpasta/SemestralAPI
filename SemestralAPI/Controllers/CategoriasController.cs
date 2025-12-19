using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Articulos;

namespace SemestralAPI.Controllers {
  //Indica que esta clase es un controlador API
  [ApiController]

  //Ruta base para todos los endpoints de artículos
  [Route("api/categorias")]
  public class CategoriasController : ControllerBase {

    //Instancia de la clase que maneja la base de datos
    private readonly BaseDatos bd;

    public CategoriasController() {
      //Inicializar la conexión a BD usando variables de entorno
      bd = new BaseDatos(
        Environment.GetEnvironmentVariable("HOST_NAME")!,
        Environment.GetEnvironmentVariable("DB_NAME")!,
        Environment.GetEnvironmentVariable("DB_USER")!,
        Environment.GetEnvironmentVariable("DB_PASSWORD")!
      );
    }

    //Obtener lista de Artículos
    [HttpGet]
    public ActionResult ObtenerCategorias() {

      //Solicitar a la BD la lista completa de artículos
      List<Categoria> lista = bd.ObtenerCategorias();

      if (lista != null) {
        return Ok(lista);
      } else {
        return StatusCode(500, "No se logró buscar a las categorías");
      }
    }

  }
}
