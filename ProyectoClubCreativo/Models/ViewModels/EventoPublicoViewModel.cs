using System.Globalization;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class EventoPublicoViewModel
    {
        private static readonly CultureInfo Cultura = new("es-CR");

        public int IdEvento { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public string? Categoria { get; set; }

        public string? ImagenUrl { get; set; }

        public DateTime FechaInicio { get; set; }
        public int? CupoMaximo { get; set; }

        public string HorarioTexto =>
            FechaFin.HasValue && FechaFin.Value > FechaInicio
                ? $"{HoraTexto} a {FechaFin.Value.ToString("h:mm tt", Cultura)}"
                : HoraTexto;

        public DateTime? FechaFin { get; set; }

        public string HoraTexto =>
            FechaInicio.ToString("h:mm tt", Cultura);

        public string FechaTexto
        {
            get
            {
                if (FechaFin is null ||
                    FechaFin.Value.Date == FechaInicio.Date)
                {
                    return FechaInicio.ToString("d 'de' MMMM", Cultura);
                }

                if (FechaFin.Value.Month == FechaInicio.Month &&
                    FechaFin.Value.Year == FechaInicio.Year)
                {
                    return $"{FechaInicio.Day} al " +
                           FechaFin.Value.ToString("d 'de' MMMM", Cultura);
                }

                return FechaInicio.ToString("d 'de' MMMM", Cultura) + " al " +
                       FechaFin.Value.ToString("d 'de' MMMM", Cultura);
            }
        }
    }
}