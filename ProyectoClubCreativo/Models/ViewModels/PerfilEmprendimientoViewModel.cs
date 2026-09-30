namespace ProyectoClubCreativo.Models.ViewModels
{
    public class PerfilEmprendimientoViewModel
    {
        public string NombreComercial { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Sin categoría";
        public string Cedula { get; set; } = string.Empty;
        public string EstadoAprobacion { get; set; } = "Pendiente";
        public bool Activo { get; set; } = true;

        public string? LogoUrl { get; set; }

        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? SitioWeb { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }

        public bool ParticipaClubCreativo { get; set; }
        public bool ParticipaHechoEnCr { get; set; }

        public List<string> Fotografias { get; set; } = new();

        public List<ProductoPerfilViewModel> ProductosDestacados { get; set; } = new();

        public int ProductosPublicados { get; set; }
        public int VentasRealizadas { get; set; }
        public int Favoritos { get; set; }
        public double? ValoracionPromedio { get; set; }
    }

    public class ProductoPerfilViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Sin categoría";
        public decimal Precio { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? ImagenUrl { get; set; }
    }
}
