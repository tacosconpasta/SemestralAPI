using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SemestralAPI.Libraries;
using SemestralAPI.Models;
using SemestralAPI.RequestParams.Ordenes;

namespace SemestralAPI.Controllers {

  [ApiController]
  [Route("api/carrito")]

  //Orden_Detalles
  public class CarritoController : ControllerBase {

    private readonly BaseDatos bd;

    public CarritoController() {
      bd = new BaseDatos(
        Environment.GetEnvironmentVariable("HOST_NAME")!,
        Environment.GetEnvironmentVariable("DB_NAME")!,
        Environment.GetEnvironmentVariable("DB_USER")!,
        Environment.GetEnvironmentVariable("DB_PASSWORD")!
      );
    }

    //Obtener detalles del carrito (orden)
    [HttpGet]
    public ActionResult<List<Orden_Detalle>> ObtenerCarrito(int ordenId) {

      if (ordenId < 0)
        return BadRequest("Enviar un id de orden válido.");

      List<Orden_Detalle> detalles = bd.ObtenerDetallesOrden(ordenId);

      if (detalles == null)
        return StatusCode(500, "No se pudieron obtener los detalles de la orden.");

      if (detalles.Count == 0)
        return NotFound("La orden no tiene artículos.");

      return Ok(detalles);
    }

    //Relacionar Artículo A Orden (Agregar a Carrito)
    [HttpPost]
    public ActionResult AgregarArticuloCarrito(AgregarArticuloCarritoRequest articuloAgregar) {
      //Validar artículo
      if (articuloAgregar == null)
        return BadRequest("Datos inválidos.");
     
      //Validar campos de artículo
      if (articuloAgregar.OrdenId < 0 ||
          articuloAgregar.ArticuloId < 0 ||
          articuloAgregar.Cantidad < 0)
        return BadRequest("Orden, artículo y cantidad deben ser válidos (mayores a 0).");

      try {
        bd.AgregarArticuloOrden(
          articuloAgregar.OrdenId,
          articuloAgregar.ArticuloId,
          articuloAgregar.Cantidad
        );

        return Ok(new {
          mensaje = "Artículo agregado al carrito correctamente"
        });

        //Manejar excepciones de triggers de SQL (No hay stock, artículo inválido..."
      } catch (PostgresException ex) {
        if (ex.SqlState == "P0002") {
          return BadRequest("No hay inventario suficiente para hacer una orden de tamaño: " + articuloAgregar.Cantidad + " de este artículo.");
        }

        if (ex.SqlState == "P0001") {
          return BadRequest("El artículo no existe.");
        }

        return StatusCode(500, "Error al agregar artículo al carrito.");
      }
    }

    //Eliminar un artículo de la relación Artículo Orden (Eliminar del carrito)
    [HttpDelete]
    public ActionResult EliminarArticuloCarrito(EliminarArticuloCarritoRequest articuloEliminar) {

      //Validación de objeto
      if (articuloEliminar == null)
        return BadRequest("Datos inválidos.");

      //Validaciones
      if (articuloEliminar.OrdenId < 0 || articuloEliminar.ArticuloId < 0)
        return BadRequest("Orden y artículo deben ser válidos (mayores a 0)");

      //Intentar eliminar artículo del carrito
      bool eliminado = bd.EliminarArticuloOrden(
          articuloEliminar.OrdenId,
          articuloEliminar.ArticuloId
      );

      //Si no se eliminó, retornar NotFound
      if (!eliminado)
        return NotFound("El artículo no existe en el carrito.");

      //Si se logró eliminar, retornar
      return Ok(new {
        mensaje = "Artículo eliminado del carrito correctamente"
      });
    }

    [HttpPut]
    public ActionResult ActualizarCarrito(EditarArticuloCarritoRequest actualizar) {

      //Validación de objeto
      if (actualizar == null)
        return BadRequest("Datos inválidos.");

      //Validaciones
      if (actualizar.OrdenId < 0 || actualizar.ArticuloId < 0 || actualizar.Cantidad < 0)
        return BadRequest("Orden, artículo y cantidad deben ser válidos (mayores a 0)");

      try {
        bool actualizado = bd.ActualizarArticuloOrden(
            actualizar.OrdenId,
            actualizar.ArticuloId,
            actualizar.Cantidad
        );

        //Si no se actualizó, no se encontró artículo.
        if (!actualizado)
          return NotFound("El artículo no existe en el carrito.");

        //Retornar OK
        return Ok(new {
          mensaje = "Carrito actualizado correctamente"
        });
      } catch (PostgresException ex) {
        //Errores del trigger de SQL (no hay stock suficiente)
        if (ex.SqlState == "P0002")
          return BadRequest("No hay inventario suficiente para actualizar el carrito.");

        return StatusCode(500, "Error al actualizar el carrito.");
      }
    }

  }
}
