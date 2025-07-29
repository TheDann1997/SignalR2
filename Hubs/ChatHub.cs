using Microsoft.AspNetCore.SignalR;
using SignalR.Data;
using SignalR.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace SignalR.Hubs
{
    public class ChatHub : Hub
    {
        //REFACTORIZADO
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }
        
        public async Task EnviarMensaje(string destinatarioId, Mensaje mensaje)
        {
            if (mensaje.EsGrupal)
            {
                Console.WriteLine($"llego un mensaje para el grupo con ID {destinatarioId}");
                await Clients.OthersInGroup($"grupo_{destinatarioId}")
                      .SendAsync("RecibirMensaje", mensaje);
            }
            else
            {
                Console.WriteLine($"llego un mensaje para el usuario con ID {destinatarioId}");
                await Clients.Group($"user_{destinatarioId}")
                      .SendAsync("RecibirMensaje", mensaje);
            }
        }

        public async Task EnviarReaccion(string nombreRemitente, string reaccion, string destinatarioId, string mensajeIdReaccion, int? grupoId = null)
        {
            try
            {
                if (grupoId != null)
                {
                    // Para grupos: enviar solo al autor del mensaje original
                    await Clients.Group($"user_{destinatarioId}")
                        .SendAsync("RecibirNotificaiondeReaccion", nombreRemitente, reaccion, mensajeIdReaccion);
                }
                else
                {
                    // Para individual: enviar al destinatario normal
                    await Clients.Group($"user_{destinatarioId}")
                        .SendAsync("RecibirNotificaiondeReaccion", nombreRemitente, reaccion, mensajeIdReaccion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en EnviarReaccion: " + ex.Message + "\n" + ex.StackTrace);
                throw;
            }
        }

        public async Task EnviarMensajeConRespuesta(Mensaje mensajerespondido, string NombreDelRemitente)
        {
            if (mensajerespondido.EsGrupal && mensajerespondido.GrupoId != null)
            {
                await Clients.Group($"grupo_{mensajerespondido.GrupoId}")
                    .SendAsync("RecibirMensajeConRespuesta", mensajerespondido, NombreDelRemitente);
            }
            else
            {
                await Clients.Group($"user_{mensajerespondido.DestinatarioId}")
                    .SendAsync("RecibirMensajeConRespuesta", mensajerespondido, NombreDelRemitente);
            }
        }

        public async Task EnviarEscribiendoMensaje(string destinatarioId,string myID, string MyAvatar)
        {
            Console.WriteLine($"escribiendo en signalR para {destinatarioId}");
           await Clients.Group($"user_{destinatarioId}")
             .SendAsync("RecibirEscribiendo", destinatarioId, myID, MyAvatar, "INDIVIDUAL");
        }
        
        public async Task EnviarEscribiendoGrupo(string grupoId, string myID, string MyAvatar)
        {
            Console.WriteLine($"escribiendo en signalR para grupo {grupoId}");
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("RecibirEscribiendo", grupoId, myID, MyAvatar, "GRUPO");
        }

        public async Task EnviarVisto(string destinatarioId, string myID)
        {
            Console.WriteLine($"Se recibio un visto para {destinatarioId}");
            await Clients.Group($"user_{destinatarioId}")
              .SendAsync("RecibirVisto", destinatarioId, myID);
        }

        public async Task EnviarMensajeVisto(int mensajeId, int grupoId, int usuarioId)
        {
            try
            {
                Console.WriteLine($"Usuario {usuarioId} vio el mensaje {mensajeId} en grupo {grupoId}");
                
                // Obtener el nombre del usuario que vio el mensaje
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
                string nombreUsuario = usuario?.Nombre ?? "Usuario";
                
                // Enviar notificación a todos los miembros del grupo
                await Clients.Group($"grupo_{grupoId}")
                    .SendAsync("RecibirMensajeVisto", mensajeId, usuarioId, nombreUsuario);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en EnviarMensajeVisto: {ex.Message}");
                throw;
            }
        }

        public async Task EnviarMensajeGrupo(string grupoId, Mensaje mensaje)
        {
            Console.WriteLine($"llego un mensaje para el grupo con ID {grupoId}");
            await Clients.Group($"grupo_{grupoId}")
                         .SendAsync("RecibirMensaje", mensaje);
        }

        public override async Task OnConnectedAsync()
        {
            // Obtener userId desde la query string
            var httpContext = Context.GetHttpContext();
            var userIdString = httpContext.Request.Query["userId"];

            if (int.TryParse(userIdString, out int userId))
            {
                // 👉 Unirse a grupo individual para mensajes directos
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                Console.WriteLine($"Usuario {userId} conectado a grupo individual user_{userId}");

                // 👉 Buscar grupos en la base de datos
                var grupos = _context.UsuariosGrupos
                    .Where(ug => ug.UsuarioId == userId)
                    .Select(ug => ug.GrupoId)
                    .ToList();

                foreach (var grupoId in grupos)
                {
                    string nombreGrupo = $"grupo_{grupoId}";
                    await Groups.AddToGroupAsync(Context.ConnectionId, nombreGrupo);
                    Console.WriteLine($"Usuario {userId} unido a grupo {nombreGrupo}");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var userIdString = httpContext.Request.Query["userId"];

            if (int.TryParse(userIdString, out int userId))
            {
                // 🔸 Remover del grupo individual
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                Console.WriteLine($"Usuario {userId} desconectado del grupo individual user_{userId}");

                // 🔸 Obtener grupos grupales desde la base de datos
                var grupos = _context.UsuariosGrupos
                    .Where(ug => ug.UsuarioId == userId)
                    .Select(ug => ug.GrupoId)
                    .ToList();

                foreach (var grupoId in grupos)
                {
                    string nombreGrupo = $"grupo_{grupoId}";
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, nombreGrupo);
                    Console.WriteLine($"Usuario {userId} removido de grupo {nombreGrupo}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task UnirseAGrupo(int grupoId)
        {
            string groupName = $"grupo_{grupoId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"✅ Usuario unido al grupo SignalR: {groupName}");
        }
        
        public async Task SalirDeGrupo(int grupoId)
        {
            string groupName = $"grupo_{grupoId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"🚪 Usuario salió del grupo SignalR: {groupName}");
        }

        public async Task EnviarLlamada(string destinatarioId, Usuario usercall)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("LlamadaEntrante", destinatarioId, usercall);
        }

        public async Task AceptarLlamada(string destinatarioId, Usuario usuario)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("LlamadaAceptada", destinatarioId, usuario);
        }

        public async Task EnviarOferta(string destinatarioId, string oferta)
        {
            string emisorId = Context.UserIdentifier;
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("RecibirOferta", oferta, emisorId);
        }

        public async Task EnviarRespuesta(string destinatarioId, string respuesta)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("RecibirRespuesta", respuesta);
        }

        public async Task EnviarIceCandidate(string destinatarioId, string candidato)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("RecibirIceCandidate", candidato);
        }

        public async Task CambiarModoPantalla(string destinatarioId, bool activo)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("ModoPantallaRemoto", activo);
        }

        public async Task ConfirmarMensajeRecibido(string remitenteId, string destinatarioId, string mensajeId)
        {
            await Clients.Group($"user_{remitenteId}").SendAsync("MensajeRecibido", destinatarioId, mensajeId);
        }
        
        public async Task ConfirmarMensajeLeido(string remitenteId, string destinatarioId, string mensajeId)
        {
            await Clients.Group($"user_{remitenteId}").SendAsync("MensajeLeido", destinatarioId, mensajeId);
        }
        
        public async Task ColgarLlamada(string destinatarioId, string remitenteId)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("LlamadaColgada", remitenteId);
        }

        // ===== NUEVOS MÉTODOS PARA LLAMADAS GRUPALES =====
        
        public async Task IniciarLlamadaGrupal(int grupoId, Usuario iniciador)
        {
            await Clients.Group($"grupo_{grupoId}")
                .SendAsync("LlamadaGrupalIniciada", grupoId, iniciador);
        }

        public async Task UnirseALlamadaGrupal(int grupoId, string userId, string ofertaSDP)
        {
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("UsuarioUnidoALlamadaGrupal", grupoId, userId, ofertaSDP);
        }

        public async Task EnviarOfertaGrupal(int grupoId, string userId, string ofertaSDP)
        {
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("RecibirOfertaGrupal", grupoId, userId, ofertaSDP);
        }

        public async Task EnviarRespuestaGrupal(int grupoId, string userId, string respuestaSDP)
        {
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("RecibirRespuestaGrupal", grupoId, userId, respuestaSDP);
        }

        public async Task EnviarIceCandidateGrupal(int grupoId, string userId, string candidato)
        {
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("RecibirIceCandidateGrupal", grupoId, userId, candidato);
        }

        public async Task SalirDeLlamadaGrupal(int grupoId, string userId)
        {
            await Clients.Group($"grupo_{grupoId}")
                .SendAsync("UsuarioSalioDeLlamadaGrupal", grupoId, userId);
        }

        public async Task TerminarLlamadaGrupal(int grupoId, string iniciadorId)
        {
            await Clients.Group($"grupo_{grupoId}")
                .SendAsync("LlamadaGrupalTerminada", grupoId, iniciadorId);
        }

        public async Task CambiarModoPantallaGrupal(int grupoId, string userId, bool activo)
        {
            await Clients.OthersInGroup($"grupo_{grupoId}")
                .SendAsync("ModoPantallaGrupalRemoto", grupoId, userId, activo);
        }

        // ===== NUEVOS MÉTODOS PARA SALAS MANUALES =====
        
        public async Task CrearSala(string salaId, string creadorId)
        {
            try
            {
                // Validar que salaId sea un número
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    await Clients.Caller.SendAsync("ErrorSala", "El ID de la sala debe ser un número");
                    return;
                }

                // Verificar si la sala ya existe
                var salaExistente = await _context.LlamadasGrupales
                    .Where(l => l.GrupoId == salaIdInt && l.Activa)
                    .FirstOrDefaultAsync();

                if (salaExistente != null)
                {
                    await Clients.Caller.SendAsync("ErrorSala", "La sala ya existe");
                    return;
                }

                // Crear nueva sala
                var sala = new LlamadaGrupal
                {
                    GrupoId = salaIdInt,
                    IniciadorId = creadorId,
                    FechaInicio = DateTime.UtcNow,
                    Activa = true,
                    MaxParticipantes = 10
                };

                _context.LlamadasGrupales.Add(sala);
                await _context.SaveChangesAsync();

                // Agregar al creador como participante
                var participante = new ParticipanteLlamada
                {
                    LlamadaGrupalId = sala.Id,
                    UsuarioId = creadorId,
                    FechaUnion = DateTime.UtcNow,
                    Activo = true,
                    CamaraActiva = true,
                    MicrofonoActivo = true
                };

                _context.ParticipantesLlamada.Add(participante);
                await _context.SaveChangesAsync();

                // Unirse al grupo de SignalR
                await Groups.AddToGroupAsync(Context.ConnectionId, $"sala_{salaIdInt}");
                
                // Notificar a todos en la sala
                await Clients.Group($"sala_{salaIdInt}")
                    .SendAsync("SalaCreada", salaIdInt.ToString(), creadorId);
                
                Console.WriteLine($"Sala {salaIdInt} creada por {creadorId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando sala: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorSala", "Error interno del servidor");
            }
        }

        public async Task UnirseASala(string salaId, string userId)
        {
            try
            {
                // Validar que salaId sea un número
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    await Clients.Caller.SendAsync("ErrorSala", "El ID de la sala debe ser un número");
                    return;
                }

                // Buscar la sala
                var sala = await _context.LlamadasGrupales
                    .Include(l => l.Participantes)
                    .Where(l => l.GrupoId == salaIdInt && l.Activa)
                    .FirstOrDefaultAsync();

                if (sala == null)
                {
                    await Clients.Caller.SendAsync("ErrorSala", "Sala no encontrada");
                    return;
                }

                // Verificar límite de participantes
                if (sala.Participantes.Count(p => p.Activo) >= sala.MaxParticipantes)
                {
                    await Clients.Caller.SendAsync("ErrorSala", "Sala llena");
                    return;
                }

                // Verificar si ya es participante
                var participanteExistente = sala.Participantes
                    .FirstOrDefault(p => p.UsuarioId == userId);

                if (participanteExistente != null)
                {
                    if (participanteExistente.Activo)
                    {
                        await Clients.Caller.SendAsync("ErrorSala", "Ya estás en la sala");
                        return;
                    }
                    else
                    {
                        // Reactivar participante
                        participanteExistente.Activo = true;
                        participanteExistente.FechaSalida = null;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Agregar nuevo participante
                    var participante = new ParticipanteLlamada
                    {
                        LlamadaGrupalId = sala.Id,
                        UsuarioId = userId,
                        FechaUnion = DateTime.UtcNow,
                        Activo = true,
                        CamaraActiva = true,
                        MicrofonoActivo = true
                    };

                    _context.ParticipantesLlamada.Add(participante);
                    await _context.SaveChangesAsync();
                }

                // Unirse al grupo de SignalR
                await Groups.AddToGroupAsync(Context.ConnectionId, $"sala_{salaIdInt}");

                // Notificar a otros participantes
                await Clients.OthersInGroup($"sala_{salaIdInt}")
                    .SendAsync("UsuarioUnidoASala", salaIdInt.ToString(), userId, "");

                // Enviar lista de participantes existentes al nuevo usuario
                var participantesActivos = sala.Participantes
                    .Where(p => p.Activo && p.UsuarioId != userId)
                    .Select(p => p.UsuarioId)
                    .ToList();

                foreach (var participanteId in participantesActivos)
                {
                    await Clients.Caller.SendAsync("ParticipanteExistente", salaIdInt.ToString(), participanteId);
                }
                
                Console.WriteLine($"Usuario {userId} se unió a la sala {salaIdInt}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uniéndose a sala: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorSala", "Error interno del servidor");
            }
        }

        public async Task SalirDeSala(string salaId, string userId)
        {
            try
            {
                // Validar que salaId sea un número
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    Console.WriteLine($"Error: salaId '{salaId}' no es un número válido");
                    return;
                }

                // Buscar participante
                var participante = await _context.ParticipantesLlamada
                    .Include(p => p.LlamadaGrupal)
                    .Where(p => p.LlamadaGrupal.GrupoId == salaIdInt && p.UsuarioId == userId)
                    .FirstOrDefaultAsync();

                if (participante != null)
                {
                    participante.Activo = false;
                    participante.FechaSalida = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Si es el iniciador, terminar la sala
                    if (participante.LlamadaGrupal.IniciadorId == userId)
                    {
                        participante.LlamadaGrupal.Activa = false;
                        participante.LlamadaGrupal.FechaFin = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        await Clients.Group($"sala_{salaIdInt}")
                            .SendAsync("SalaEliminada", salaIdInt.ToString());
                    }
                    else
                    {
                        await Clients.OthersInGroup($"sala_{salaIdInt}")
                            .SendAsync("UsuarioSalioDeSala", salaIdInt.ToString(), userId);
                    }
                }

                // Salir del grupo de SignalR
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sala_{salaIdInt}");
                
                Console.WriteLine($"Usuario {userId} salió de la sala {salaIdInt}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saliendo de sala: {ex.Message}");
            }
        }

        public async Task EnviarOfertaSala(string salaId, string destinatarioId, string ofertaSDP)
        {
            await Clients.Group($"sala_{salaId}")
                .SendAsync("RecibirOfertaSala", salaId, destinatarioId, ofertaSDP);
        }

        public async Task EnviarRespuestaSala(string salaId, string destinatarioId, string respuestaSDP)
        {
            await Clients.Group($"sala_{salaId}")
                .SendAsync("RecibirRespuestaSala", salaId, destinatarioId, respuestaSDP);
        }

        public async Task ObtenerParticipantesSala(string salaId)
        {
            try
            {
                var participantes = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.GrupoId.ToString() == salaId && p.Activo)
                    .Select(p => new
                    {
                        UsuarioId = p.UsuarioId,
                        FechaUnion = p.FechaUnion,
                        CamaraActiva = p.CamaraActiva,
                        MicrofonoActivo = p.MicrofonoActivo,
                        CompartiendoPantalla = p.CompartiendoPantalla
                    })
                    .ToListAsync();

                await Clients.Caller.SendAsync("ListaParticipantes", salaId, participantes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo participantes: {ex.Message}");
            }
        }

        public async Task EnviarIceCandidateSala(string salaId, string destinatarioId, string candidato)
        {
            await Clients.Group($"sala_{salaId}")
                .SendAsync("RecibirIceCandidateSala", salaId, destinatarioId, candidato);
        }
    }
} 