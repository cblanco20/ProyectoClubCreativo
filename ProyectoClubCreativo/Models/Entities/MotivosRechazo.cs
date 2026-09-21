using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class MotivosRechazo
{
    public int IdMotivoRechazo { get; set; }

    public int? IdEmprendimiento { get; set; }

    public int? IdParticipacion { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaRechazo { get; set; }

    public virtual Emprendimiento? IdEmprendimientoNavigation { get; set; }

    public virtual ParticipacionesEvento? IdParticipacionNavigation { get; set; }
}
