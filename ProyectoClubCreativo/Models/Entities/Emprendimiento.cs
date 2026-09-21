using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Emprendimiento
{
    public int IdEmprendimiento { get; set; }

    public int IdUsuarioPropietario { get; set; }

    public int? IdCategoria { get; set; }

    public string NombreComercial { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? CedulaJuridica { get; set; }

    public string? Telefono { get; set; }

    public string Correo { get; set; } = null!;

    public string? SitioWeb { get; set; }

    public string? Instagram { get; set; }

    public string? Facebook { get; set; }

    public string? LogoUrl { get; set; }

    public bool ParticipaClubCreativo { get; set; }

    public bool ParticipaHechoEnCr { get; set; }

    public string EstadoAprobacion { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<ComentariosResena> ComentariosResenas { get; set; } = new List<ComentariosResena>();

    public virtual EmprendimientoImagene? EmprendimientoImagene { get; set; }

    public virtual EmprendimientoRevisione? EmprendimientoRevisione { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual ICollection<Galeria> Galeria { get; set; } = new List<Galeria>();

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual Usuario IdUsuarioPropietarioNavigation { get; set; } = null!;

    public virtual ICollection<MotivosRechazo> MotivosRechazos { get; set; } = new List<MotivosRechazo>();

    public virtual ICollection<ParticipacionesEvento> ParticipacionesEventos { get; set; } = new List<ParticipacionesEvento>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual Suscripcione? Suscripcione { get; set; }

    public virtual ICollection<Etiqueta> IdEtiqueta { get; set; } = new List<Etiqueta>();

    public virtual ICollection<Promocione> IdPromocions { get; set; } = new List<Promocione>();
}
