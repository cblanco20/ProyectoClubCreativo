using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class EmprendimientoImagene
{
    public int IdImagen { get; set; }

    public int IdEmprendimiento { get; set; }

    public string UrlImagen { get; set; } = null!;

    public string? NombreArchivo { get; set; }

    public bool EsPrincipal { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;
}
