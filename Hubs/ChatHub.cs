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
            try
            {
                var httpContext = Context.GetHttpContext();
                var userId = httpContext?.Request.Query["userId"].ToString();
                
                Console.WriteLine($"🔗 Usuario conectado: {userId} (ConnectionId: {Context.ConnectionId})");
                Console.WriteLine($"🔑 Context.UserIdentifier: {Context.UserIdentifier}");
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Guardar el userId en el contexto para uso posterior
                    Context.Items["userId"] = userId;
                    
                    // Agregar al usuario a su grupo personal para mensajes directos
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                    Console.WriteLine($"✅ Usuario {userId} agregado a su grupo personal: user_{userId}");
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en OnConnectedAsync: {ex.Message}");
                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                var userIdString = httpContext?.Request.Query["userId"].ToString();

                if (!string.IsNullOrEmpty(userIdString))
                {
                    // Buscar en qué salas está el usuario y marcarlo como inactivo
                    var participacionesActivas = await _context.ParticipantesLlamada
                        .Include(p => p.LlamadaGrupal)
                        .Where(p => p.UsuarioId == userIdString && p.Activo)
                        .ToListAsync();

                    foreach (var participacion in participacionesActivas)
                    {
                        participacion.Activo = false;
                        participacion.FechaSalida = DateTime.UtcNow;

                        // Si es el iniciador, terminar la sala
                        if (participacion.LlamadaGrupal.IniciadorId == userIdString)
                        {
                            participacion.LlamadaGrupal.Activa = false;
                            participacion.LlamadaGrupal.FechaFin = DateTime.UtcNow;
                        }
                        else
                        {
                            // Notificar a otros participantes
                            await Clients.OthersInGroup($"grupo_{participacion.LlamadaGrupal.GrupoId}")
                                .SendAsync("UsuarioSalioDeSala", participacion.LlamadaGrupal.GrupoId.ToString(), userIdString);
                        }
                    }

                    if (participacionesActivas.Any())
                    {
                        await _context.SaveChangesAsync();
                    }

                    // Remover de grupos de SignalR usando formato consistente
                    var grupos = await _context.ParticipantesLlamada
                        .Where(p => p.UsuarioId == userIdString && p.Activo == false)
                        .Select(p => $"grupo_{p.LlamadaGrupal.GrupoId}")
                        .Distinct()
                        .ToListAsync();

                    foreach (var grupo in grupos)
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
                    }
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en OnDisconnectedAsync: {ex.Message}");
                await base.OnDisconnectedAsync(exception);
            }
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

                // Unirse al grupo de SignalR usando el formato consistente
                await Groups.AddToGroupAsync(Context.ConnectionId, $"grupo_{salaIdInt}");
                
                // Notificar a todos en la sala usando el formato consistente
                await Clients.Group($"grupo_{salaIdInt}")
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

                // Unirse al grupo de SignalR usando formato consistente
                await Groups.AddToGroupAsync(Context.ConnectionId, $"grupo_{salaIdInt}");

                // Notificar a otros participantes que hay un nuevo usuario (sin oferta)
                await Clients.OthersInGroup($"grupo_{salaIdInt}")
                    .SendAsync("NuevoUsuarioEnSala", salaIdInt.ToString(), userId);

                // Enviar lista de participantes existentes al nuevo usuario
                var participantesActivos = sala.Participantes
                    .Where(p => p.Activo && p.UsuarioId != userId)
                    .Select(p => p.UsuarioId)
                    .ToList();

                foreach (var participanteId in participantesActivos)
                {
                    await Clients.Caller.SendAsync("ParticipanteExistente", salaIdInt.ToString(), participanteId);
                }

                // Notificar al nuevo usuario que debe enviar ofertas a los participantes existentes
                if (participantesActivos.Any())
                {
                    await Clients.Caller.SendAsync("EnviarOfertasAExistentes", salaIdInt.ToString(), participantesActivos);
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

                        await Clients.Group($"grupo_{salaIdInt}")
                            .SendAsync("SalaEliminada", salaIdInt.ToString());
                    }
                    else
                    {
                        await Clients.OthersInGroup($"grupo_{salaIdInt}")
                            .SendAsync("UsuarioSalioDeSala", salaIdInt.ToString(), userId);
                    }
                }

                // Salir del grupo de SignalR usando formato consistente
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"grupo_{salaIdInt}");
                
                Console.WriteLine($"Usuario {userId} salió de la sala {salaIdInt}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saliendo de sala: {ex.Message}");
            }
        }

        public async Task EnviarOfertaSala(string salaId, string destinatarioId, string ofertaSDP)
        {
            Console.WriteLine($"📤 Enviando oferta de {Context.UserIdentifier} a {destinatarioId} en sala {salaId}");
            Console.WriteLine($"🔑 Context.UserIdentifier: '{Context.UserIdentifier}'");
            Console.WriteLine($"🎯 Destinatario: '{destinatarioId}'");
            
            try
            {
                // Verificar si el destinatario está conectado
                var httpContext = Context.GetHttpContext();
                Console.WriteLine($"🌐 HttpContext disponible: {httpContext != null}");
                
                // Enviar solo al destinatario específico, no a todo el grupo
                await Clients.User(destinatarioId).SendAsync("RecibirOfertaSala", salaId, Context.UserIdentifier, ofertaSDP);
                Console.WriteLine($"✅ Oferta enviada de {Context.UserIdentifier} a {destinatarioId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error enviando oferta a {destinatarioId}: {ex.Message}");
                // Intentar enviar al grupo como fallback
                try
                {
                    Console.WriteLine($"🔄 Intentando enviar al grupo grupo_{salaId} como fallback");
                    await Clients.Group($"grupo_{salaId}").SendAsync("RecibirOfertaSala", salaId, Context.UserIdentifier, ofertaSDP);
                    Console.WriteLine($"✅ Oferta enviada al grupo como fallback");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"❌ Error en fallback: {ex2.Message}");
                }
            }
        }

        public async Task EnviarRespuestaSala(string salaId, string destinatarioId, string respuestaSDP)
        {
            Console.WriteLine($"📤 Enviando respuesta de {Context.UserIdentifier} a {destinatarioId} en sala {salaId}");
            Console.WriteLine($"🔑 Context.UserIdentifier: '{Context.UserIdentifier}'");
            Console.WriteLine($"🎯 Destinatario: '{destinatarioId}'");
            
            // Enviar solo al destinatario específico, no a todo el grupo
            await Clients.User(destinatarioId).SendAsync("RecibirRespuestaSala", salaId, Context.UserIdentifier, respuestaSDP);
            Console.WriteLine($"✅ Respuesta enviada de {Context.UserIdentifier} a {destinatarioId}");
        }

        public async Task ObtenerParticipantesSala(string salaId)
        {
            try
            {
                // Validar que salaId sea un número
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    return;
                }

                var participantes = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.GrupoId == salaIdInt && p.Activo)
                    .Select(p => new
                    {
                        UsuarioId = p.UsuarioId,
                        FechaUnion = p.FechaUnion,
                        CamaraActiva = p.CamaraActiva,
                        MicrofonoActivo = p.MicrofonoActivo,
                        CompartiendoPantalla = p.CompartiendoPantalla
                    })
                    .ToListAsync();

                Console.WriteLine($"📋 Enviando lista de {participantes.Count} participantes a {Context.UserIdentifier}");
                await Clients.Caller.SendAsync("ListaParticipantes", salaIdInt.ToString(), participantes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo participantes: {ex.Message}");
            }
        }

        public async Task EnviarIceCandidateSala(string salaId, string destinatarioId, string candidato)
        {
            Console.WriteLine($"🧊 Enviando candidato ICE de {Context.UserIdentifier} a {destinatarioId} en sala {salaId}");
            // Enviar solo al destinatario específico, no a todo el grupo
            await Clients.User(destinatarioId).SendAsync("RecibirIceCandidateSala", salaId, Context.UserIdentifier, candidato);
            Console.WriteLine($"✅ Candidato ICE enviado de {Context.UserIdentifier} a {destinatarioId}");
        }

        // Método de debug para probar comunicación directa
        public async Task DebugEnviarMensaje(string destinatarioId, string mensaje)
        {
            Console.WriteLine($"🐛 Debug: Enviando mensaje de {Context.UserIdentifier} a {destinatarioId}: {mensaje}");
            Console.WriteLine($"🔑 Context.UserIdentifier: '{Context.UserIdentifier}'");
            Console.WriteLine($"🎯 Destinatario: '{destinatarioId}'");
            
            try
            {
                await Clients.User(destinatarioId).SendAsync("DebugRecibirMensaje", Context.UserIdentifier, mensaje);
                Console.WriteLine($"✅ Debug: Mensaje enviado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Debug: Error enviando mensaje: {ex.Message}");
            }
        }

        // Método de debug para listar usuarios conectados
        public async Task DebugListarUsuarios()
        {
            Console.WriteLine($"🔍 Debug: Usuario {Context.UserIdentifier} solicitando lista de usuarios");
            Console.WriteLine($"🔑 Context.UserIdentifier: '{Context.UserIdentifier}'");
            
            // Enviar confirmación al usuario que solicitó la lista
            await Clients.Caller.SendAsync("DebugListaUsuarios", $"Usuario {Context.UserIdentifier} está conectado");
        }

        // Método de fallback para enviar a todo el grupo
        public async Task DebugEnviarAGrupo(string salaId, string mensaje)
        {
            Console.WriteLine($"🐛 Debug Grupo: Enviando mensaje de {Context.UserIdentifier} a grupo grupo_{salaId}: {mensaje}");
            
            try
            {
                await Clients.Group($"grupo_{salaId}").SendAsync("DebugRecibirMensajeGrupo", Context.UserIdentifier, mensaje);
                Console.WriteLine($"✅ Debug Grupo: Mensaje enviado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Debug Grupo: Error enviando mensaje: {ex.Message}");
            }
        }

        // Método de debug específico para salas
        public async Task DebugEnviarMensajeSala(string salaId, string mensaje)
        {
            Console.WriteLine($"🐛 Debug Sala: Enviando mensaje de {Context.UserIdentifier} a sala {salaId}: {mensaje}");
            
            try
            {
                await Clients.Group($"grupo_{salaId}").SendAsync("DebugRecibirMensajeSala", Context.UserIdentifier, mensaje);
                Console.WriteLine($"✅ Debug Sala: Mensaje enviado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Debug Sala: Error enviando mensaje: {ex.Message}");
            }
        }

        // Método para verificar si un usuario está conectado
        public async Task VerificarUsuarioConectado(string userId)
        {
            Console.WriteLine($"🔍 Verificando si usuario {userId} está conectado");
            Console.WriteLine($"🔑 Context.UserIdentifier: '{Context.UserIdentifier}'");
            
            try
            {
                // Intentar enviar un mensaje de prueba al usuario
                await Clients.User(userId).SendAsync("DebugRecibirMensaje", Context.UserIdentifier, "Mensaje de prueba de conectividad");
                Console.WriteLine($"✅ Usuario {userId} está conectado y recibió el mensaje");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Usuario {userId} no está conectado o no pudo recibir el mensaje: {ex.Message}");
            }
        }

        // Método para forzar el envío de ofertas
        public async Task ForzarEnvioOfertas(string salaId)
        {
            try
            {
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    Console.WriteLine($"❌ salaId '{salaId}' no es un número válido");
                    return;
                }

                Console.WriteLine($"🔄 Forzando envío de ofertas en sala {salaId} por usuario {Context.UserIdentifier}");

                // Obtener todos los participantes activos en la sala
                var participantes = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.GrupoId == salaIdInt && p.Activo && p.UsuarioId != Context.UserIdentifier)
                    .Select(p => p.UsuarioId)
                    .ToListAsync();

                Console.WriteLine($"📋 Participantes encontrados: {string.Join(", ", participantes)}");

                // Notificar a todos los participantes que deben enviar ofertas
                foreach (var participanteId in participantes)
                {
                    Console.WriteLine($"📤 Notificando a {participanteId} que debe enviar oferta a {Context.UserIdentifier}");
                    await Clients.User(participanteId).SendAsync("ForzarOfertaA", salaId, Context.UserIdentifier);
                }

                Console.WriteLine($"✅ Notificaciones enviadas a {participantes.Count} participantes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error forzando envío de ofertas: {ex.Message}");
            }
        }

        // Método para verificar el estado de las conexiones peer
        public async Task<object> VerificarConexionesPeer(string salaId)
        {
            try
            {
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    return new { error = "salaId no válido" };
                }

                Console.WriteLine($"🔍 Verificando conexiones peer en sala {salaId} para usuario {Context.UserIdentifier}");

                // Obtener todos los participantes activos en la sala
                var participantes = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.GrupoId == salaIdInt && p.Activo)
                    .Select(p => new
                    {
                        UsuarioId = p.UsuarioId,
                        FechaUnion = p.FechaUnion,
                        Activo = p.Activo
                    })
                    .ToListAsync();

                var resultado = new
                {
                    salaId = salaId,
                    usuarioActual = Context.UserIdentifier,
                    totalParticipantes = participantes.Count,
                    participantes = participantes,
                    timestamp = DateTime.UtcNow
                };

                Console.WriteLine($"📊 Estado de conexiones: {participantes.Count} participantes activos");
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error verificando conexiones peer: {ex.Message}");
                return new { error = ex.Message };
            }
        }

        // Método para solicitar ofertas de participantes existentes
        public async Task SolicitarOfertasAExistentes(string salaId)
        {
            try
            {
                if (!int.TryParse(salaId, out int salaIdInt))
                {
                    Console.WriteLine($"❌ salaId '{salaId}' no es un número válido");
                    return;
                }

                Console.WriteLine($"🔄 Usuario {Context.UserIdentifier} solicita ofertas de participantes existentes en sala {salaId}");

                // Obtener todos los participantes activos en la sala (excluyendo al solicitante)
                var participantes = await _context.ParticipantesLlamada
                    .Where(p => p.LlamadaGrupal.GrupoId == salaIdInt && p.Activo && p.UsuarioId != Context.UserIdentifier)
                    .Select(p => p.UsuarioId)
                    .ToListAsync();

                if (participantes.Any())
                {
                    Console.WriteLine($"📋 Participantes existentes en sala {salaId}: {string.Join(", ", participantes)}");
                    
                    // Notificar a cada participante existente que debe enviar una oferta al nuevo usuario
                    foreach (var participanteId in participantes)
                    {
                        try
                        {
                            await Clients.User(participanteId.ToString()).SendAsync("ForzarOfertaA", salaId, Context.UserIdentifier);
                            Console.WriteLine($"✅ Notificación enviada a participante {participanteId} para enviar oferta a {Context.UserIdentifier}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error notificando a participante {participanteId}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ No hay participantes existentes en sala {salaId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error solicitando ofertas de participantes existentes: {ex.Message}");
            }
        }

        // Método para notificar cambios de estado (cámara, micrófono, pantalla)
        public async Task NotificarCambioEstado(string salaId, string userId, string tipo, bool estado)
        {
            try
            {
                Console.WriteLine($"📢 Usuario {userId} cambió estado de {tipo} a {(estado ? "activado" : "desactivado")} en sala {salaId}");
                
                // Notificar a todos los participantes en la sala (excepto al remitente)
                await Clients.OthersInGroup($"grupo_{salaId}").SendAsync("CambioEstadoParticipante", salaId, userId, tipo, estado);
                
                Console.WriteLine($"✅ Notificación de cambio de estado enviada a sala {salaId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error notificando cambio de estado: {ex.Message}");
            }
        }
    }
} 