using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class RespuestaDetalle
{
    public long IdRespuestaDetalle { get; set; }

    public long IdRespuestaEncuesta { get; set; }

    public int IdPregunta { get; set; }

    public int? IdOpcion { get; set; }

    public string? RespuestaTexto { get; set; }

    public decimal? ValorNumerico { get; set; }

    public virtual PreguntasEncuestum IdPreguntaNavigation { get; set; } = null!;

    public virtual RespuestasEncuestum IdRespuestaEncuestaNavigation { get; set; } = null!;

    public virtual OpcionesPreguntum? OpcionesPreguntum { get; set; }
}
