using System;
using System.Collections.Generic;

namespace SignalR.Models
{
    public class LlamadaGrupal
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public string IniciadorId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activa { get; set; }
        public int MaxParticipantes { get; set; } = 10; // Límite por defecto
        
        // Propiedades de navegación
        public virtual ICollection<ParticipanteLlamada> Participantes { get; set; }
    }

    public class ParticipanteLlamada
    {
        public int Id { get; set; }
        public int LlamadaGrupalId { get; set; }
        public string UsuarioId { get; set; }
        public DateTime FechaUnion { get; set; }
        public DateTime? FechaSalida { get; set; }
        public bool Activo { get; set; }
        public bool CamaraActiva { get; set; } = true;
        public bool MicrofonoActivo { get; set; } = true;
        public bool CompartiendoPantalla { get; set; } = false;
        
        // Propiedades de navegación
        public virtual LlamadaGrupal LlamadaGrupal { get; set; }
    }
} 