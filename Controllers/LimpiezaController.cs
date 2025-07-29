using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalR.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LimpiezaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LimpiezaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("limpiar-datos-antiguos")]
        public async Task<IActionResult> LimpiarDatosAntiguos([FromQuery] int diasRetencion = 365)
        {
            try
            {
                var fechaLimite = DateTime.UtcNow.AddDays(-diasRetencion);

                // Obtener estadísticas antes de la limpieza
                var llamadasAntiguas = await _context.LlamadasGrupales
                    .Where(l => l.FechaFin.HasValue && l.FechaFin < fechaLimite)
                    .CountAsync();

                var participantesAntiguos = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.FechaFin.HasValue && p.LlamadaGrupal.FechaFin < fechaLimite)
                    .CountAsync();

                // Obtener IDs de llamadas antiguas
                var idsLlamadasAntiguas = await _context.LlamadasGrupales
                    .Where(l => l.FechaFin.HasValue && l.FechaFin < fechaLimite)
                    .Select(l => l.Id)
                    .ToListAsync();

                if (idsLlamadasAntiguas.Any())
                {
                    // Eliminar participantes de llamadas antiguas
                    var participantesEliminados = await _context.ParticipantesLlamada
                        .Where(p => idsLlamadasAntiguas.Contains(p.LlamadaGrupalId))
                        .ExecuteDeleteAsync();

                    // Eliminar llamadas antiguas
                    var llamadasEliminadas = await _context.LlamadasGrupales
                        .Where(l => idsLlamadasAntiguas.Contains(l.Id))
                        .ExecuteDeleteAsync();

                    return Ok(new
                    {
                        mensaje = "Limpieza completada exitosamente",
                        llamadasEliminadas,
                        participantesEliminados,
                        diasRetencion,
                        fechaLimite
                    });
                }
                else
                {
                    return Ok(new
                    {
                        mensaje = "No hay datos antiguos para limpiar",
                        diasRetencion,
                        fechaLimite
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error durante la limpieza",
                    mensaje = ex.Message
                });
            }
        }

        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            try
            {
                var totalLlamadas = await _context.LlamadasGrupales.CountAsync();
                var llamadasActivas = await _context.LlamadasGrupales.CountAsync(l => l.Activa);
                var llamadasCompletadas = await _context.LlamadasGrupales.CountAsync(l => !l.Activa);
                var totalParticipantes = await _context.ParticipantesLlamada.CountAsync();

                var llamadasAntiguas = await _context.LlamadasGrupales
                    .Where(l => l.FechaFin.HasValue && l.FechaFin < DateTime.UtcNow.AddDays(-365))
                    .CountAsync();

                return Ok(new
                {
                    totalLlamadas,
                    llamadasActivas,
                    llamadasCompletadas,
                    totalParticipantes,
                    llamadasAntiguas,
                    fechaConsulta = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error obteniendo estadísticas",
                    mensaje = ex.Message
                });
            }
        }
    }
}