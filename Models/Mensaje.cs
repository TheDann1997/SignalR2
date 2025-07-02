namespace SignalR.Models
{
    public class Mensaje
    {

       
            public int Id { get; set; }
            public int RemitenteId { get; set; }
            public int? DestinatarioId { get; set; }
            public bool EsGrupal { get; set; }
            public string Tipo { get; set; }
            public string Texto { get; set; }
            public string archivo_url { get; set; }
            public string nombre_archivo { get; set; }
            public int? GrupoId { get; set; }
            public DateTime FechaEnvio { get; set; }
            public string Estado { get; set; }

        public string AvatarRemitente { get; set; }
        public string NombreRemitente { get; set; }
        public string NombreGrupo { get; set; }

        public int? RespondeAId { get; set; }
        public Mensaje? MensajeRespondido { get; set; }
    }
}
