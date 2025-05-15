using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class ChatHub : Hub
    {
        public async Task EnviarMensajePrivado(string destinatarioId, string remitente, string mensaje)
        {

      
            // Envia solo al grupo del destinatario
            await Clients.Group($"user_{destinatarioId}")
                         .SendAsync("RecibirMensaje", remitente, mensaje);
           // await Clients.All.SendAsync("ActualizarContadorMensajes");
        }
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




        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                Console.WriteLine($"Usuario {userId} conectado al grupo user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                Console.WriteLine($"Usuario {userId} desconectado del grupo user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }


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

    }
}
