namespace ProyectoClubCreativo.Models.ViewModels
{
    public class EstadisticasEmprendedorViewModel
    {
        public int Periodo { get; set; } = 30;
        public string DescripcionPeriodo { get; set; } = string.Empty;

        public decimal Ventas { get; set; }
        public decimal VentasAnterior { get; set; }
        public double VariacionVentas { get; set; }
        public int Ordenes { get; set; }
        public double VariacionOrdenes { get; set; }
        public int Unidades { get; set; }
        public double VariacionUnidades { get; set; }
        public int Favoritos { get; set; }
        public double VariacionFavoritos { get; set; }

        public double? Valoracion { get; set; }
        public int TotalValoraciones { get; set; }
        public int Clientes { get; set; }
        public decimal TicketPromedio { get; set; }
        public int ProductosPublicados { get; set; }
        public int ProductosTotales { get; set; }

        public List<string> EscalaVentas { get; set; } = [];
        public List<string> EtiquetasVentas { get; set; } = [];
        public string RutaActual { get; set; } = string.Empty;
        public string RutaAnterior { get; set; } = string.Empty;
        public string RutaArea { get; set; } = string.Empty;
        public List<PuntoGraficoEmprendedorViewModel> Puntos { get; set; } = [];

        public string GradienteCategorias { get; set; } = "conic-gradient(#e5e7eb 0 100%)";
        public List<CategoriaVentaViewModel> Categorias { get; set; } = [];

        public List<ProductoEstadisticaViewModel> TopProductos { get; set; } = [];
        public List<ProductoEstadisticaViewModel> Productos { get; set; } = [];
        public List<ActividadEstadisticaViewModel> Actividad { get; set; } = [];

        public int ProductosBajoStock { get; set; }
        public string? ProductoEstrella { get; set; }
    }

    public class PuntoGraficoEmprendedorViewModel
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class CategoriaVentaViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ProductoEstadisticaViewModel
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string? ImagenUrl { get; set; }
        public int Stock { get; set; }
        public int Favoritos { get; set; }
        public int Unidades { get; set; }
        public int Ordenes { get; set; }
        public decimal Ingresos { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ActividadEstadisticaViewModel
    {
        public string Tipo { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}