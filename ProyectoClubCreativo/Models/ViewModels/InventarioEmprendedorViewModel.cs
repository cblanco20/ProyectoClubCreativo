using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class InventarioEmprendedorViewModel
    {
        public AjustarInventarioViewModel Ajuste { get; set; } = new();

        public List<ProductoInventarioViewModel> Productos { get; set; } = new();

        public int UnidadesDisponibles { get; set; }

        public int ProductosDisponibles { get; set; }

        public int ProductosInventarioBajo { get; set; }

        public int ProductosAgotados { get; set; }
    }

    public class ProductoInventarioViewModel
    {
        public int IdProducto { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string ImagenUrl { get; set; } = string.Empty;

        public int StockActual { get; set; }

        public string EstadoInventario { get; set; } = string.Empty;
    }
}