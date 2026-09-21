using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Noticia
{
    public int IdNoticia { get; set; }

    public int IdAutor { get; set; }

    public int? IdCategoria { get; set; }

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string? ImagenUrl { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public virtual Usuario IdAutorNavigation { get; set; } = null!;

    public virtual Categoria? IdCategoriaNavigation { get; set; }
}
