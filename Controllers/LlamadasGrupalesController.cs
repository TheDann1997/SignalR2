using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalR.Data;
using SignalR.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LlamadasGrupalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LlamadasGrupalesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LlamadasGrupales/grupo/{grupoId}
        [HttpGet("grupo/{grupoId}")]
        public async Task<IActionResult> GetLlamadaActiva(int grupoId)
        {
            var llamada = await _context.LlamadasGrupales
                .Include(l => l.Participantes)
                .Where(l => l.GrupoId == grupoId && l.Activa)
                .FirstOrDefaultAsync();

            if (llamada == null)
                return NotFound("No hay llamada activa para este grupo");

            return Ok(llamada);
        }

        // POST: api/LlamadasGrupales/iniciar
        [HttpPost("iniciar")]
        public async Task<IActionResult> IniciarLlamada([FromBody] IniciarLlamadaRequest request)
        {
            // Verificar si ya hay una llamada activa
            var llamadaExistente = await _context.LlamadasGrupales
                .Where(l => l.GrupoId == request.GrupoId && l.Activa)
                .FirstOrDefaultAsync();

            if (llamadaExistente != null)
                return BadRequest("Ya hay una llamada activa para este grupo");

            // Verificar que el usuario pertenece al grupo
            var esMiembro = await _context.UsuariosGrupos
                .AnyAsync(ug => ug.GrupoId == request.GrupoId && ug.UsuarioId.ToString() == request.IniciadorId);

            if (!esMiembro)
                return BadRequest("El usuario no pertenece a este grupo");

            var llamada = new LlamadaGrupal
            {
                GrupoId = request.GrupoId,
                IniciadorId = request.IniciadorId,
                FechaInicio = DateTime.UtcNow,
                Activa = true,
                MaxParticipantes = request.MaxParticipantes ?? 10
            };

            _context.LlamadasGrupales.Add(llamada);
            await _context.SaveChangesAsync();

            // Agregar al iniciador como participante
            var participante = new ParticipanteLlamada
            {
                LlamadaGrupalId = llamada.Id,
                UsuarioId = request.IniciadorId,
                FechaUnion = DateTime.UtcNow,
                Activo = true,
                CamaraActiva = true,
                MicrofonoActivo = true
            };

            _context.ParticipantesLlamada.Add(participante);
            await _context.SaveChangesAsync();

            return Ok(new { llamadaId = llamada.Id, mensaje = "Llamada iniciada correctamente" });
        }

        // POST: api/LlamadasGrupales/unirse
        [HttpPost("unirse")]
        public async Task<IActionResult> UnirseALlamada([FromBody] UnirseLlamadaRequest request)
        {
            var llamada = await _context.LlamadasGrupales
                .Include(l => l.Participantes)
                .Where(l => l.Id == request.LlamadaId && l.Activa)
                .FirstOrDefaultAsync();

            if (llamada == null)
                return NotFound("Llamada no encontrada o no activa");

            // Verificar límite de participantes
            if (llamada.Participantes.Count(p => p.Activo) >= llamada.MaxParticipantes)
                return BadRequest("Llamada llena");

            // Verificar que el usuario pertenece al grupo
            var esMiembro = await _context.UsuariosGrupos
                .AnyAsync(ug => ug.GrupoId == llamada.GrupoId && ug.UsuarioId.ToString() == request.UsuarioId);

            if (!esMiembro)
                return BadRequest("El usuario no pertenece a este grupo");

            // Verificar si ya es participante
            var participanteExistente = llamada.Participantes
                .FirstOrDefault(p => p.UsuarioId == request.UsuarioId);

            if (participanteExistente != null)
            {
                if (participanteExistente.Activo)
                    return BadRequest("Ya estás en la llamada");
                else
                {
                    // Reactivar participante
                    participanteExistente.Activo = true;
                    participanteExistente.FechaSalida = null;
                    await _context.SaveChangesAsync();
                    return Ok("Reconectado a la llamada");
                }
            }

            // Agregar nuevo participante
            var participante = new ParticipanteLlamada
            {
                LlamadaGrupalId = request.LlamadaId,
                UsuarioId = request.UsuarioId,
                FechaUnion = DateTime.UtcNow,
                Activo = true,
                CamaraActiva = true,
                MicrofonoActivo = true
            };

            _context.ParticipantesLlamada.Add(participante);
            await _context.SaveChangesAsync();

            return Ok("Unido a la llamada correctamente");
        }

        // POST: api/LlamadasGrupales/salir
        [HttpPost("salir")]
        public async Task<IActionResult> SalirDeLlamada([FromBody] SalirLlamadaRequest request)
        {
            var participante = await _context.ParticipantesLlamada
                .Include(p => p.LlamadaGrupal)
                .Where(p => p.LlamadaGrupalId == request.LlamadaId && p.UsuarioId == request.UsuarioId)
                .FirstOrDefaultAsync();

            if (participante == null)
                return NotFound("Participante no encontrado");

            participante.Activo = false;
            participante.FechaSalida = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Si es el iniciador, terminar la llamada
            if (participante.LlamadaGrupal.IniciadorId == request.UsuarioId)
            {
                participante.LlamadaGrupal.Activa = false;
                participante.LlamadaGrupal.FechaFin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok("Llamada terminada");
            }

            return Ok("Salido de la llamada");
        }

        // PUT: api/LlamadasGrupales/estado
        [HttpPut("estado")]
        public async Task<IActionResult> ActualizarEstado([FromBody] ActualizarEstadoRequest request)
        {
            var participante = await _context.ParticipantesLlamada
                .Where(p => p.LlamadaGrupalId == request.LlamadaId && p.UsuarioId == request.UsuarioId)
                .FirstOrDefaultAsync();

            if (participante == null)
                return NotFound("Participante no encontrado");

            if (request.CamaraActiva.HasValue)
                participante.CamaraActiva = request.CamaraActiva.Value;

            if (request.MicrofonoActivo.HasValue)
                participante.MicrofonoActivo = request.MicrofonoActivo.Value;

            if (request.CompartiendoPantalla.HasValue)
                participante.CompartiendoPantalla = request.CompartiendoPantalla.Value;

            await _context.SaveChangesAsync();
            return Ok("Estado actualizado");
        }

        // GET: api/LlamadasGrupales/participantes/{llamadaId}
        [HttpGet("participantes/{llamadaId}")]
        public async Task<IActionResult> GetParticipantes(int llamadaId)
        {
            var participantes = await _context.ParticipantesLlamada
                .Where(p => p.LlamadaGrupalId == llamadaId && p.Activo)
                .Select(p => new
                {
                    p.UsuarioId,
                    p.CamaraActiva,
                    p.MicrofonoActivo,
                    p.CompartiendoPantalla,
                    p.FechaUnion
                })
                .ToListAsync();

            return Ok(participantes);
        }
    }

    // DTOs para las requests
    public class IniciarLlamadaRequest
    {
        public int GrupoId { get; set; }
        public string IniciadorId { get; set; }
        public int? MaxParticipantes { get; set; }
    }

    public class UnirseLlamadaRequest
    {
        public int LlamadaId { get; set; }
        public string UsuarioId { get; set; }
    }

    public class SalirLlamadaRequest
    {
        public int LlamadaId { get; set; }
        public string UsuarioId { get; set; }
    }

    public class ActualizarEstadoRequest
    {
        public int LlamadaId { get; set; }
        public string UsuarioId { get; set; }
        public bool? CamaraActiva { get; set; }
        public bool? MicrofonoActivo { get; set; }
        public bool? CompartiendoPantalla { get; set; }
    }
} 