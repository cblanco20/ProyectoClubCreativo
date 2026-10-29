namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ReporteAsistenciaViewModel
    {
        public int IdActividad { get; set; }

        public string NombreActividad { get; set; } = "";

        public string TipoActividad { get; set; } = "";

        public List<AsistenteReporteViewModel> Asistentes { get; set; } = new();
    }

    public class AsistenteReporteViewModel
    {
        public string Nombre { get; set; } = "";

        public string Correo { get; set; } = "";

        public DateTime FechaHora { get; set; }
    }
}