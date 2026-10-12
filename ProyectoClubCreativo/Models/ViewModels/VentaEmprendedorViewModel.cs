namespace ProyectoClubCreativo.Models.ViewModels
{
    public class VentaEmprendedorViewModel
    {
        public int IdVenta { get; set; }

        public string NumeroOrden { get; set; } = string.Empty;

        public string NombreComprador { get; set; } = string.Empty;

        public string CorreoComprador { get; set; } = string.Empty;

        public DateTime FechaVenta { get; set; }

        public string Productos { get; set; } = string.Empty;

        public int CantidadProductos { get; set; }

        public decimal Monto { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string MetodoPago { get; set; } = string.Empty;

        public string Iniciales =>
            string.IsNullOrWhiteSpace(NombreComprador)
                ? "?"
                : string.Concat(
                    NombreComprador
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Take(2)
                        .Select(p => char.ToUpperInvariant(p[0])));

        public string EstadoClave => Estado switch
        {
            "Pendiente" => "pendiente",
            "Pagada" => "pendiente",
            "En preparacion" => "preparacion",
            "Enviada" => "enviada",
            "Entregada" => "entregada",
            "Cancelada" => "cancelada",
            _ => "pendiente"
        };

        public string EstadoTexto =>
            Estado == "En preparacion" ? "En preparación" : Estado;

        public string FiltroFecha
        {
            get
            {
                DateTime hoy = DateTime.Today;

                if (FechaVenta.Date == hoy)
                {
                    return "hoy";
                }

                int diasDesdeLunes =
                    ((int)hoy.DayOfWeek + 6) % 7;

                if (FechaVenta.Date >= hoy.AddDays(-diasDesdeLunes))
                {
                    return "semana";
                }

                if (FechaVenta.Year == hoy.Year &&
                    FechaVenta.Month == hoy.Month)
                {
                    return "mes";
                }

                return "antiguo";
            }
        }
    }
}