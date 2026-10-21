using System.Globalization;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class TallerPublicoViewModel
    {
        private static readonly CultureInfo Cultura = new("es-CR");

        public int IdTaller { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string? ImagenUrl { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int CupoMaximo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int Inscritos { get; set; }
        public string? ResponsableNombre { get; set; }
        public string? ResponsableCorreo { get; set; }
        public string? ResponsableTelefono { get; set; }
        public bool UsuarioRegistrado { get; set; }

        public int CuposDisponibles => Math.Max(CupoMaximo - Inscritos, 0);
        public bool CupoLleno => CuposDisponibles == 0;
        public bool PocosCupos => ReglasTalleres.QuedanPocosCupos(CuposDisponibles);

        public string TextoDisponibilidad =>
            CupoLleno ? "Cupo completo"
            : PocosCupos ? "Últimos cupos disponibles"
            : "Cupos disponibles";

        public string FechaTexto => FechaInicio.ToString("dddd d 'de' MMMM", Cultura);
        public string HoraTexto => FechaInicio.ToString("h:mm tt", Cultura);
        public string HorarioTexto =>
            FechaFin.HasValue && FechaFin.Value > FechaInicio
                ? $"{HoraTexto} a {FechaFin.Value.ToString("h:mm tt", Cultura)}"
                : HoraTexto;
    }

    public class ConfirmacionInscripcionViewModel
    {
        public string Tipo { get; set; } = "Taller";
        public int IdActividad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Horario { get; set; } = string.Empty;
        public string Lugar { get; set; } = string.Empty;
        public string? ResponsableNombre { get; set; }
        public string? ResponsableCorreo { get; set; }
        public string? ResponsableTelefono { get; set; }
    }

    public class MiTallerViewModel
    {
        public int IdTaller { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Horario { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string? ImagenUrl { get; set; }
        public string Estado { get; set; } = "Próximo";
        public string? ResponsableNombre { get; set; }
    }

    public class MisTalleresViewModel
    {
        public string Filtro { get; set; } = "Todos";
        public int Total { get; set; }
        public List<MiTallerViewModel> Talleres { get; set; } = [];
    }

    public class CancelarRegistroTallerViewModel
    {
        public int IdTaller { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Horario { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public bool MenosDe24Horas { get; set; }
    }
}
