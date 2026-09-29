namespace ProyectoClubCreativo.Models.ViewModels
{
    public class EstadoSolicitudViewModel
    {
        public string NombreComercial { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string? SitioWeb { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public string Estado { get; set; } = "Pendiente";

        public DateTime? FechaSolicitud { get; set; }

        public DateTime? FechaResolucion { get; set; }

        public string? MotivoRechazo { get; set; }
    }
}
