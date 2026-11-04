namespace ProyectoClubCreativo.Models.ViewModels
{
    public class DetalleEmprendimientoPublicoViewModel
    {
        public int IdEmprendimiento { get; set; }

        public string NombreComercial { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string? Categoria { get; set; }

        public string? Provincia { get; set; }

        public string? LogoUrl { get; set; }

        public string? Telefono { get; set; }

        public string Correo { get; set; } = string.Empty;

        public string? SitioWeb { get; set; }

        public string? Instagram { get; set; }

        public string? Facebook { get; set; }

        public double? Valoracion { get; set; }

        public int CantidadResenas { get; set; }

        public List<string> Imagenes { get; set; } = new();

        public List<ProductoEmprendimientoPublicoViewModel> Productos { get; set; } = new();

        public List<EmprendimientoPublicoViewModel> Relacionados { get; set; } = new();

        public string? EnlaceWhatsApp
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Telefono))
                {
                    return null;
                }

                string digitos = new(Telefono.Where(char.IsDigit).ToArray());

                if (digitos.Length == 8)
                {
                    digitos = "506" + digitos;
                }

                return digitos.Length >= 8
                    ? $"https://wa.me/{digitos}"
                    : null;
            }
        }
    }

    public class ProductoEmprendimientoPublicoViewModel
    {
        public int IdProducto { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public string? ImagenUrl { get; set; }
    }
}
