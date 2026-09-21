using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class ParticipacionesEvento
{
    public int IdParticipacion { get; set; }

    public int IdEvento { get; set; }

    public int IdEmprendimiento { get; set; }

    public string? TipoEspacio { get; set; }

    public string? ProductosPresentados { get; set; }

    public string? NecesidadesEspeciales { get; set; }

    public bool RequiereElectricidad { get; set; }

    public bool RequiereMesa { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual ICollection<MotivosRechazo> MotivosRechazos { get; set; } = new List<MotivosRechazo>();
}
