using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdEmprendimiento { get; set; }

    public int? IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string TipoPublicacion { get; set; } = null!;

    public decimal Precio { get; set; }

    public int StockActual { get; set; }

    public bool EsDestacado { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<CarritoDetalle> CarritoDetalles { get; set; } = new List<CarritoDetalle>();

    public virtual ICollection<ComentariosResena> ComentariosResenas { get; set; } = new List<ComentariosResena>();

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual ICollection<Galeria> Galeria { get; set; } = new List<Galeria>();

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual ProductoImagene? ProductoImagene { get; set; }

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();

    public virtual ICollection<Etiqueta> IdEtiqueta { get; set; } = new List<Etiqueta>();

    public virtual ICollection<Promocione> IdPromocions { get; set; } = new List<Promocione>();
}
