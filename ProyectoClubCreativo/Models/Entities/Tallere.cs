using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Tallere
{
    public int IdTaller { get; set; }

    public int? IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Ubicacion { get; set; }

    public int CupoMaximo { get; set; }

    public string? ImagenUrl { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual ICollection<InscripcionesTaller> InscripcionesTallers { get; set; } = new List<InscripcionesTaller>();

    public virtual ICollection<RegistrosAsistencium> RegistrosAsistencia { get; set; } = new List<RegistrosAsistencium>();

    public virtual ICollection<Encuesta> IdEncuesta { get; set; } = new List<Encuesta>();

    public virtual ICollection<Etiqueta> IdEtiqueta { get; set; } = new List<Etiqueta>();
}
