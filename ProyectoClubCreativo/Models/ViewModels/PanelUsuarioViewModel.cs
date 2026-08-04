namespace ProyectoClubCreativo.Models.ViewModels
{
    public class PanelUsuarioViewModel
    {
        public string NombreUsuario { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string FotoPerfil { get; set; } = string.Empty;

        public int PuntosAcumulados { get; set; }

        public int CantidadFavoritos { get; set; }

        public int NotificacionesPendientes { get; set; }

        public List<EventoUsuarioViewModel> ProximosEventos { get; set; } = [];

        public List<TallerUsuarioViewModel> TalleresReservados { get; set; } = [];

        public List<PedidoUsuarioViewModel> PedidosRecientes { get; set; } = [];

        public List<NotificacionUsuarioViewModel> Notificaciones { get; set; } = [];
    }

    public class EventoUsuarioViewModel
    {
        public string Nombre { get; set; } = string.Empty;

        public string Fecha { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public string Imagen { get; set; } = string.Empty;
    }

    public class TallerUsuarioViewModel
    {
        public string Nombre { get; set; } = string.Empty;

        public string Fecha { get; set; } = string.Empty;

        public string Hora { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;
    }

    public class PedidoUsuarioViewModel
    {
        public string NumeroOrden { get; set; } = string.Empty;

        public string Fecha { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public string Estado { get; set; } = string.Empty;
    }

    public class NotificacionUsuarioViewModel
    {
        public string Tipo { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public string Fecha { get; set; } = string.Empty;

        public string Icono { get; set; } = string.Empty;
    }
}
