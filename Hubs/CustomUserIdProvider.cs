// Archivo: CustomUserIdProvider.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Hubs
{
    /// <summary>
    /// Extrae el userId de la query string (?userId=…) para que Clients.User(id) funcione.
    /// </summary>
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Lee el parámetro "userId" de la URL de conexión
         

            var httpContext = connection.GetHttpContext();
            return httpContext?.Request.Query["userId"];

        }
    }
}
