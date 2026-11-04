namespace ProyectoClubCreativo.Models.ViewModels
    {
        public class EmprendimientoPublicoViewModel
        {
            public int IdEmprendimiento { get; set; }
            public string NombreComercial { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string? Categoria { get; set; }
            public string? Provincia { get; set; }
            public string? ImagenUrl { get; set; }
            public double? Valoracion { get; set; }
        }
    }