using Microsoft.AspNetCore.Mvc;
using SignalR.Models; // <-- Asegúrate de importar tu modelo

namespace SignalR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivosController : ControllerBase
    {
        [HttpPost("Subir")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Subir([FromForm] SubidaArchivoModel model)
        {
            var archivo = model.Archivo;
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo vacío");

            string carpetaUploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(carpetaUploads))
                Directory.CreateDirectory(carpetaUploads);

            string nombreUnico = $"{Guid.NewGuid()}_{archivo.FileName}";
            string ruta = Path.Combine(carpetaUploads, nombreUnico);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            string url = $"/uploads/{nombreUnico}";
            return Ok(new { url, nombreOriginal = archivo.FileName });
        }


        [HttpGet("Descargar/{nombreArchivo}")]
        public IActionResult DescargarArchivo(string nombreArchivo)
        {
            // Ruta absoluta del archivo dentro de la carpeta 'uploads'
            var rutaArchivo = Path.Combine(Directory.GetCurrentDirectory(), "uploads", nombreArchivo);

            if (!System.IO.File.Exists(rutaArchivo))
                return NotFound("Archivo no encontrado");

            // Leemos el archivo como arreglo de bytes
            var bytes = System.IO.File.ReadAllBytes(rutaArchivo);

            // Forzamos la descarga usando el tipo MIME 'application/octet-stream'
            return File(bytes, "application/octet-stream", nombreArchivo);
        }


    }
}