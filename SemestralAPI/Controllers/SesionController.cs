using Microsoft.AspNetCore.Mvc;

namespace SemestralAPI.Controllers {

  [ApiController]
  
  [Route("api/[controller]")]
  public class SesionController : Controller {
    
    [HttpPost("verify")]
    public ActionResult VerificarSesion() {
      return Ok(new { status_code = 200 });
    }
  }
}
