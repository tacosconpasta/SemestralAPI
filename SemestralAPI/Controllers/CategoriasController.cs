using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Articulos;
using SemestralAPI.RequestParams.Articulos.Categorias;

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
    public ActionResult<List<Categoria>> ObtenerCategorias() {

      //Solicitar a la BD la lista completa de artículos
      List<Categoria> lista = bd.ObtenerCategorias();

      if (lista != null) {
        return Ok(lista);
      } else {
        return StatusCode(500, "No se logró buscar a las categorías");
      }
    }

    //Obtener categoría por Id
    [HttpGet("{id:int}")]
    public ActionResult BuscarCategoria(int id) {
      if (id <= 0) { 
        return BadRequest("No hay ninguna categoría con un id menor o igual a 0");
      }

      Categoria categoria = bd.ObtenerCategoria(id);

      //Si la categoría existe
      if (categoria != null) {
        return Ok(categoria);
      } else {
        return NotFound("No se encontró ninguna categoría con ese id.");
      }

    }

    //Obtener categoría con todos sus hijos, recursivamente
    [HttpGet("{id:int}/arbol")]
    public ActionResult ObtenerCategoriaArbol(int id) {
      if (id <= 0)
        return BadRequest("El id de la categoría debe ser mayor a 0.");

      CategoriaArbol arbol = bd.ObtenerCategoriaArbol(id);

      if (arbol == null)
        return NotFound("La categoría indicada no existe.");

      return Ok(arbol);
    }

    //Buscar categorías por nombre
    [HttpGet("buscar")]
    public ActionResult<List<Categoria>> BuscarCategorias([FromQuery] string nombre) {

      if (string.IsNullOrWhiteSpace(nombre))
        return BadRequest("Debe enviar un nombre para buscar.");

      List<Categoria> categorias = bd.BuscarCategorias(nombre);

      if (categorias == null)
        return StatusCode(500, "Hubo un error al buscar las categorías.");

      if (categorias.Count == 0)
        return NotFound("No se encontraron categorías con ese nombre.");

      return Ok(categorias);
    }

    [HttpPost]
    public ActionResult AñadirCategoria(InsertarCategoriaRequest categoria) { 
      if(categoria == null) 
        return BadRequest("Por favor, enviar una categoría a insertar.");

      //Identificar si la categoría ya existe
      Categoria yaExiste = bd.ObtenerCategoria(categoria.Nombre);

      //Si la categoría ya existe
      if (yaExiste != null) {
        return Conflict("La categoría que deseas insertar ya existe, con id = " + yaExiste.Id);
      }

      bool fueInsertada = bd.AgregarCategoria(categoria.Nombre, categoria.CategoriaPadreId);

      if (fueInsertada) {
        return Ok("La categoría fue agregada exitosamente!");
      } else {
        return StatusCode(500, "No se pudo insertar la categoría, revisar si los campos son válidos. Si la categoría no tiene categoría padre, su padreId debe ser 0");
      }
    }

    [HttpDelete]
    public ActionResult EliminarCategoria(int categoria_id) {
      if (categoria_id <= 0)
        return BadRequest("Por favor, enviar un id válido para la categoría a eliminar.");

      Categoria categoria = bd.ObtenerCategoria(categoria_id);

      //Si la categoria NO existe
      if (categoria == null) {
        return NotFound("La categoría que desea eliminar no existe.");
      }

      //Si la categoría existe, eliminarla
      bool fueEliminada = bd.EliminarCategoria(categoria.Id);

      if (!fueEliminada)
        return StatusCode(500, "Hubo un error al intentar eliminar la categoría.");

      return Ok("La categoría a eliminar fue eliminada exitosamente");
    }

    [HttpPut]
    public ActionResult ActualizarCategoria(Categoria categoria) {
      if (categoria == null)
        return BadRequest("Por favor, enviar una categoría a insertar.");

      //Identificar si la categoría existe
      Categoria categoriaExiste = bd.ObtenerCategoria(categoria.Id);

      //Si la categoria no existe
      if (categoriaExiste == null)
        return NotFound("La categoría que deseas actualizar no existe.");

      //Si la categoría existe
      Categoria categoriaActualizada = bd.EditarCategoria(categoria);

      //Si la categoría no fue actualizada
      if (categoriaActualizada == null)
        return StatusCode(500, "Hubo un error al actualizar la categoría.");

      return Ok(categoriaActualizada);

    }
  }
}
