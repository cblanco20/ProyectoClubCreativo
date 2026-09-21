using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class ProductoImagene
{
    public int IdImagen { get; set; }

    public int IdProducto { get; set; }

    public string UrlImagen { get; set; } = null!;

    public string? NombreArchivo { get; set; }

    public byte OrdenVisual { get; set; }

    public bool EsPrincipal { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
