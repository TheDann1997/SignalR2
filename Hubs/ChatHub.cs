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


        // ... existing code ...
        public async Task EnviarReaccion(string nombreRemitente, string reaccion, string destinatarioId, string mensajeIdReaccion, int? grupoId = null)
        {
            try
            {
                if (grupoId != null)
                {
                    await Clients.Group($"grupo_{grupoId}")
                        .SendAsync("RecibirNotificaiondeReaccion", nombreRemitente, reaccion, mensajeIdReaccion);
                }
                else
                {
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
        // ... existing code ...

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



       // public async Task EnviarReaccion(string nombreRemitente, string reaccion, string destinatarioId, string mensajeIdReaccion)
      // {
      //    
      //
      //     //Console.WriteLine($"llego una reaccion para el usuario con ID {destinatarioId}");
      //     await Clients.Group($"user_{destinatarioId}")
      //       .SendAsync("RecibirNotificaiondeReaccion", nombreRemitente, reaccion, mensajeIdReaccion);
      // }
     //  public async Task EnviarMensajeConRespuesta(Mensaje mensajerespondido, string NombreDelRemitente)
     //  {
     //      if (mensajerespondido.EsGrupal)
     //      {
     //         // await Clients.OthersInGroup($"grupo_{mensajerespondido.GrupoId}")
     //   // .SendAsync("RecibirMensajeConRespuesta", mensajerespondido);
     //      }
     //      else
     //      {
     //          await Clients.Group($"user_{mensajerespondido.DestinatarioId}")
     //         .SendAsync("RecibirMensajeConRespuesta", mensajerespondido, NombreDelRemitente);
     //      }
     //
     //      //Console.WriteLine($"llego una reaccion para el usuario con ID {destinatarioId}");
     //     
     //  }



        public async Task EnviarEscribiendoMensaje(string destinatarioId,string myID, string MyAvatar)
        {


            Console.WriteLine($"escribiendo en signalR para {destinatarioId}");
           await Clients.Group($"user_{destinatarioId}")
             .SendAsync("RecibirEscribiendo", destinatarioId, myID, MyAvatar);
        }


        public async Task EnviarVisto(string destinatarioId, string myID)
        {


            Console.WriteLine($"Se recibio un visto para {destinatarioId}");
            await Clients.Group($"user_{destinatarioId}")
              .SendAsync("RecibirVisto", destinatarioId, myID);
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
           // Console.WriteLine($"Llamada aceptada  para {destinatarioId}");
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
