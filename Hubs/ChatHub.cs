using Microsoft.AspNetCore.SignalR;
using SignalR.Data;
using SignalR.Models;
using System;
using System.Threading.Tasks;

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
                //Console.WriteLine($"llego un mensaje para el grupo con ID {destinatarioId}");
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
        public async Task EnviarReaccion(string nombreRemitente, string reaccion, string destinatarioId, string mensajeIdReaccion)
        {
           

            //Console.WriteLine($"llego una reaccion para el usuario con ID {destinatarioId}");
            await Clients.Group($"user_{destinatarioId}")
              .SendAsync("RecibirNotificaiondeReaccion", nombreRemitente, reaccion, mensajeIdReaccion);
        }
        public async Task EnviarMensajeConRespuesta(Mensaje mensajerespondido, string NombreDelRemitente)
        {
            if (mensajerespondido.EsGrupal)
            {
               // await Clients.OthersInGroup($"grupo_{mensajerespondido.GrupoId}")
         // .SendAsync("RecibirMensajeConRespuesta", mensajerespondido);
            }
            else
            {
                await Clients.Group($"user_{mensajerespondido.DestinatarioId}")
               .SendAsync("RecibirMensajeConRespuesta", mensajerespondido, NombreDelRemitente);
            }

            //Console.WriteLine($"llego una reaccion para el usuario con ID {destinatarioId}");
           
        }
        public async Task EnviarEscribiendoMensaje(string destinatarioId,string myID, string MyAvatar)
        {


            Console.WriteLine($"escribiendo en signalR para {destinatarioId}");
           await Clients.Group($"user_{destinatarioId}")
             .SendAsync("RecibirEscribiendo", destinatarioId, myID, MyAvatar);
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


        //  public async Task EnviarMensajePrivado(string destinatarioId, string remitente, string mensaje)
        //  {
        //
        //
        //      // Envia solo al grupo del destinatario
        //      await Clients.Group($"user_{destinatarioId}")
        //                   .SendAsync("RecibirMensaje", remitente, mensaje);
        //      // await Clients.All.SendAsync("ActualizarContadorMensajes");
        //  }
        public async Task EnviarGifPrivado(string destinatarioId, string remitenteId, string gifUrl)
        {

            if (string.IsNullOrEmpty(destinatarioId) || string.IsNullOrEmpty(remitenteId) || string.IsNullOrEmpty(gifUrl))
                return;

            try
            {
                await Clients.Group($"user_{destinatarioId}")
                    .SendAsync("RecibirGifPrivado", remitenteId, gifUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al enviar GIF privado: {ex.Message}");
            }
        }


        public async Task EnviarAudioPrivado(string destinatarioId, string remitenteNombre, byte[] audioData)
        {
            if (string.IsNullOrEmpty(destinatarioId) || string.IsNullOrEmpty(remitenteNombre) || audioData == null || audioData.Length == 0)
                return;

            // await Clients.User(destinatarioId).SendAsync("RecibirAudioPrivado", remitenteNombre, audioData);

            await Clients.Group($"user_{destinatarioId}")
             .SendAsync("RecibirAudioPrivado", remitenteNombre, audioData);

        }
        public async Task EnviarArchivoPrivado(string destinatarioId, string remitenteNombre, byte[] archivoBytes, string nombreArchivo)
        {
            Console.WriteLine($"🟢 EnviarArchivoPrivado ejecutado: destinatario={destinatarioId}, remitente={remitenteNombre}, archivo={nombreArchivo}");

            if (string.IsNullOrEmpty(destinatarioId) || archivoBytes == null || archivoBytes.Length == 0)
                return;

            // await Clients.User(destinatarioId).SendAsync("RecibirArchivoPrivado", remitenteNombre, archivoBytes, nombreArchivo);
            await Clients.Group($"user_{destinatarioId}")
             .SendAsync("RecibirArchivoPrivado", remitenteNombre, archivoBytes, nombreArchivo);

        }
        public async Task EnviarMensajeAGrupo(int grupoId, int remitenteId, string remitenteNombre, string mensajeTexto)
        {
            Console.WriteLine($"📨 Mensaje grupal de {remitenteNombre} (ID {remitenteId}) para grupo {grupoId}: {mensajeTexto}");

            await Clients.Group($"grupo_{grupoId}")
                         .SendAsync("RecibirMensajeGrupo", grupoId, remitenteId, remitenteNombre, mensajeTexto);
        }
        public async Task EnviarGifAGrupo(int grupoId, int remitenteId, string remitenteNombre, string gifUrl)
        {
            Console.WriteLine($"🎁 GIF grupal de {remitenteNombre} para grupo {grupoId}: {gifUrl}");

            // Envía el gif a todos los miembros del grupo, incluyendo quien lo envió
            await Clients.Group($"grupo_{grupoId}")
                         .SendAsync("RecibirGifGrupo", grupoId, remitenteId, remitenteNombre, gifUrl);
        }
        public async Task EnviarAudioAGrupo(int grupoId, int remitenteId, string remitenteNombre, byte[] audioBytes)
        {
            Console.WriteLine($"🔊 Audio grupal de {remitenteNombre} para grupo {grupoId} (tamaño: {audioBytes?.Length} bytes)");

            // Envía el audio a todos los miembros del grupo, incluyendo al remitente
            await Clients.Group($"grupo_{grupoId}")
                         .SendAsync("RecibirAudioGrupo", grupoId, remitenteId, remitenteNombre, audioBytes);
        }

        public async Task EnviarArchivoGrupo(int grupoId, int remitenteId, string remitenteNombre, byte[] archivoBytes, string nombreArchivo)
        {
            if (archivoBytes == null || archivoBytes.Length == 0)
            {
                Console.WriteLine("❌ El archivo recibido está vacío o es nulo.");
                return;
            }

            string nombreGrupo = $"Grupo_{grupoId}";

            Console.WriteLine($"📁 Archivo enviado por {remitenteNombre} al grupo {grupoId} (Nombre del archivo: {nombreArchivo}, Tamaño: {archivoBytes.Length} bytes)");

            await Clients.Group(nombreGrupo).SendAsync(
                "RecibirArchivoGrupo",
                grupoId,
                remitenteId,
                remitenteNombre,
                archivoBytes,
                nombreArchivo
            );
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




      // public override async Task OnConnectedAsync()
      // {
      //     var httpContext = Context.GetHttpContext();
      //     var userId = httpContext.Request.Query["userId"];
      //
      //     if (!string.IsNullOrEmpty(userId))
      //     {
      //         await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
      //         Console.WriteLine($"Usuario {userId} conectado al grupo user_{userId}");
      //     }
      //
      //     await base.OnConnectedAsync();
       // }

       // public override async Task OnDisconnectedAsync(Exception exception)
       // {
       //     var httpContext = Context.GetHttpContext();
       //     var userId = httpContext.Request.Query["userId"];
       //
       //     if (!string.IsNullOrEmpty(userId))
       //     {
       //         await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
       //         Console.WriteLine($"Usuario {userId} desconectado del grupo user_{userId}");
       //     }
       //
       //     await base.OnDisconnectedAsync(exception);
       // }










        public async Task NotificarLlamadaEntrante(string destinatarioId, string remitenteId, string remitenteNombre)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("LlamadaEntrante", remitenteId, remitenteNombre);
        }

        public async Task AceptarLlamada(string remitenteId, string destinatarioId, string remitenteNombre)
        {
            await Clients.Group($"user_{remitenteId}")
                .SendAsync("LlamadaAceptada", destinatarioId, remitenteNombre);
        }

        public async Task TransmitirAudioEnVivo(string destinatarioId, string remitenteId, byte[] audioData)
        {
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("RecibirAudioEnVivo", remitenteId, audioData);
        }

        public async Task NotificarVideollamadaEntrante(string destinatarioId, string remitenteId, string remitenteNombre)
        {
            // Validar que el destinatario esté conectado y en el grupo
            await Clients.Group($"user_{destinatarioId}")
                .SendAsync("VideollamadaEntrante", remitenteId, remitenteNombre);
        }

        public async Task AceptarVideollamada(string remitenteId, string destinatarioNombre)
        {
            await Clients.Group($"user_{remitenteId}")
                .SendAsync("VideollamadaAceptada", destinatarioNombre);
        }


        public async Task RechazarVideollamada(string remitenteId, string destinatarioNombre)
        {
            await Clients.Group($"user_{remitenteId}")
                .SendAsync("VideollamadaRechazada", destinatarioNombre);
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

        //opcionale para ams adekante

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



    }
}
