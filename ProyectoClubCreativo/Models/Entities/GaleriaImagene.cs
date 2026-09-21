using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class GaleriaImagene
{
    public int IdImagen { get; set; }

    public int IdGaleria { get; set; }

    public string UrlImagen { get; set; } = null!;

    public string? NombreArchivo { get; set; }

    public bool EsPortada { get; set; }

    public byte OrdenVisual { get; set; }

    public virtual Galeria IdGaleriaNavigation { get; set; } = null!;
}
