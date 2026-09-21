using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Encuesta
{
    public int IdEncuesta { get; set; }

    public int IdCreador { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual Usuario IdCreadorNavigation { get; set; } = null!;

    public virtual ICollection<PreguntasEncuestum> PreguntasEncuesta { get; set; } = new List<PreguntasEncuestum>();

    public virtual ICollection<RespuestasEncuestum> RespuestasEncuesta { get; set; } = new List<RespuestasEncuestum>();

    public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();

    public virtual ICollection<Tallere> IdTallers { get; set; } = new List<Tallere>();
}
