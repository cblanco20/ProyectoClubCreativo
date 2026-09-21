using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Categoria
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Modulo { get; set; } = null!;

    public bool Activa { get; set; }

    public virtual ICollection<Emprendimiento> Emprendimientos { get; set; } = new List<Emprendimiento>();

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual ICollection<Galeria> Galeria { get; set; } = new List<Galeria>();

    public virtual ICollection<Noticia> Noticia { get; set; } = new List<Noticia>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<Tallere> Talleres { get; set; } = new List<Tallere>();
}
