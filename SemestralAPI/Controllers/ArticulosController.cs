using Microsoft.AspNetCore.Mvc;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Articulos;
using SemestralAPI.RequestParams.Articulos.Categorias;

namespace SemestralAPI.Controllers {

  //Indica que esta clase es un controlador API
  [ApiController]

  //Ruta base para todos los endpoints de artículos
  [Route("api/articulos")]
  public class ArticulosController : ControllerBase {

    //Instancia de la clase que maneja la base de datos
    private readonly BaseDatos bd;

    public ArticulosController() {
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
    public ActionResult ObtenerArticulos() {

      // Solicitar a la BD la lista completa de artículos
      var lista = bd.ObtenerArticulos();

      // Devolver la lista
      return Ok(new {
        articulos = lista
      });
    }

    //Obtener un artículo por ID
    [HttpGet("{id:int}")]
    public ActionResult ObtenerArticuloPorId(int id) {

      //Buscar el artículo en la BD por su ID
      var articulo = bd.ObtenerArticuloPorId(id);

      //Responder 404 si no existe
      if (articulo == null)
        return NotFound(new { message = $"Artículo con id {id} no encontrado." });

      //Devolver el artículo si existe
      return Ok(articulo);
    }

    //Crear Artículo
    [HttpPost]
    public ActionResult CrearArticulo([FromBody] InsertarArticuloRequest req) {

      //Validar que el body cumpla el modelo
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      //Construir el objeto Articulo con los datos recibidos
      var nuevoArticulo = new Articulo {
        Nombre = req.Nombre,
        Descripcion = req.Descripcion,
        Precio = req.Precio,
        Stock = req.Stock,
        Paga_itbms = req.PagaITBMS
      };

      //Enviar a la BD para crear el artículo
      var creado = bd.CrearArticulo(nuevoArticulo);

      //Si hubo error en la creación, devolver 500
      if (creado == null)
        return StatusCode(500, "No se pudo crear el artículo.");

      //Devolver 201 Created con la ubicación del nuevo recurso
      return CreatedAtAction(
          nameof(ObtenerArticuloPorId),
          new { id = creado.Id },
          creado
      );
    }

    //Editar un Artículo
    [HttpPut("{id:int}")]
    public ActionResult EditarArticulo(int id, [FromBody] ActualizarArticuloRequest req) {
      //Validar que el body cumpla el modelo
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      //Verificar que el artículo exista en la BD
      var existente = bd.ObtenerArticuloPorId(id);

      //Responder 404 si no existe
      if (existente == null)
        return NotFound(new { message = $"Artículo con id {id} no existe." });

      //Actualizar los campos solo si fueron enviados
      existente.Nombre = req.Nombre ?? existente.Nombre;
      existente.Descripcion = req.Descripcion ?? existente.Descripcion;
      existente.Precio = req.Precio ?? existente.Precio;
      existente.Stock = req.Stock ?? existente.Stock;
      existente.Paga_itbms = req.PagaITBMS ?? existente.Paga_itbms;

      //Guardar cambios en la BD
      var actualizado = bd.EditarArticulo(existente);

      //Si la BD no actualizó, devolver 500
      if (actualizado == null)
        return StatusCode(500, "No se pudo actualizar el artículo.");

      //Devolver el artículo actualizado (que es el mismo que fue enviado en el req)
      return Ok(actualizado);
    }

    
    //Eliminar un artículo
    [HttpDelete("{id:int}")]
    public ActionResult EliminarArticulo(int id) {

      //Verificar que el artículo exista antes de eliminar
      var existente = bd.ObtenerArticuloPorId(id);

      //Si no existe
      if (existente == null)
        return NotFound(new { message = $"Artículo con id {id} no existe." });

      //Intentar eliminar el artículo en la BD
      bool eliminado = bd.EliminarArticulo(id);

      //Si la BD falló al eliminar, devolver 500
      if (!eliminado)
        return StatusCode(500, "No se pudo eliminar el artículo.");

      //Confirmar la eliminación
      return Ok(new { message = "Artículo eliminado correctamente." });
    }

    //Buscar por nombre
    [HttpPost("buscar")]
    public ActionResult BuscarArticulosPorNombre([FromBody] BuscarArticuloByNameRequest parametros) {

      //Validar parametros válido
      if (string.IsNullOrWhiteSpace(parametros.Nombre))
        return BadRequest(new { mensaje = "Debe proporcionar un nombre para la búsqueda." });

      //Solicitar a bd artículos
      var lista = bd.BuscarArticulosPorNombre(parametros.Nombre);

      //Devolver la lista encontrada junto a la cantidad
      return Ok(new {
        articulos = lista
      });
    }

    //Filtrar artículo por categoría
    [HttpGet("filtrar/{categoria_id:int}")]
    public ActionResult FiltrarArticulosPorCategoria(int categoria_id) {

      if (categoria_id <= 0)
        return BadRequest(new { mensaje = "El id de la categoría debe ser válido." });

      var categoria = bd.ObtenerCategoria(categoria_id);

      if (categoria == null)
        return NotFound(new { mensaje = "La categoría no existe." });

      //Filtrar artículos
      var articulos = bd.ObtenerArticulosPorCategoriaRecursiva(categoria_id);

      if (articulos == null)
        return StatusCode(500, "No se pudieron obtener los artículos.");

      //Retornar objeto con información relevante para renderización por paginado
      return Ok(new {
        categoriaFiltro = new {
          categoria.Id,
          categoria.Nombre
        },
        total = articulos.Count,
        articulos
      });
    }


    //Obtener categorías de Artículo
    [HttpGet("{id:int}/categorias")]
    public ActionResult ObtenerCategoriasArticulo(int id) {

      if (id <= 0)
        return BadRequest("Id de artículo inválido.");

      //Verificar que el artículo exista
      var articulo = bd.ObtenerArticuloPorId(id);
      if (articulo == null)
        return NotFound($"No existe un artículo con id {id}.");

      //Obtener categorías
      var categorias = bd.ObtenerCategoriasPorArticulo(id);

      if (categorias == null)
        return StatusCode(500, "No se pudieron obtener las categorías del artículo.");

      return Ok(categorias);
    }

    //Asignar categorías a un artículo
    [HttpPost("{articuloId:int}/categorias")]
    public ActionResult AsignarCategorias(int articuloId, [FromBody] AgregarCategoriasArticuloRequest req) {

      //Validación id
      if (articuloId <= 0)
        return BadRequest("Id de artículo inválido.");

      //Si no vinieron categorías
      if (req?.CategoriasIds == null || req.CategoriasIds.Count == 0)
        return BadRequest("Debe enviar al menos una categoría.");

      //Si se logró asignar las categorías
      bool asignado = bd.AsignarCategoriasArticulo(articuloId, req.CategoriasIds);

      //Si no se asignaron
      if (!asignado)
        return StatusCode(500, "No se pudieron asignar las categorías.");

      //Retornar el id artículo y categorías
      return Ok(new {
        mensaje = "Categorías asignadas correctamente.",
        articuloId = articuloId,
        categorias = req.CategoriasIds
      });
    }
  }
}
