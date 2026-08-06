using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels.Admin
{
    // ---------- ACCESO ----------
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = string.Empty;
    }

    // ---------- DASHBOARD ----------
    public class DashboardAdminViewModel
    {
        public int TotalUsuarios { get; set; }
        public int EmprendimientosRegistrados { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int ProductosActivos { get; set; }
        public decimal VentasTotal { get; set; }
        public int VariacionVentasPorcentaje { get; set; }

        public FiltroDashboardViewModel Filtro { get; set; } = new();

        public List<PuntoGraficoViewModel> VentasPorMes { get; set; } = [];
        public List<PuntoGraficoViewModel> ProductosPorCategoria { get; set; } = [];

        public List<EventoAdminViewModel> ProximosEventos { get; set; } = [];
        public List<TallerAdminViewModel> TalleresDisponibles { get; set; } = [];
        public List<AlertaAdminViewModel> Alertas { get; set; } = [];
        public List<ActividadAdminViewModel> ActividadReciente { get; set; } = [];
    }

    public class FiltroDashboardViewModel
    {
        [Display(Name = "Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicio { get; set; }

        [Display(Name = "Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Evento")]
        public string? Evento { get; set; }

        [Display(Name = "Emprendimiento")]
        public string? Emprendimiento { get; set; }
    }

    public class PuntoGraficoViewModel
    {
        public string Etiqueta { get; set; } = string.Empty;
        public int Valor { get; set; }
    }

  
    public class AlertaAdminViewModel
    {
        public string Tipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Icono { get; set; } = "bi-exclamation-triangle-fill";
        public string Nivel { get; set; } = "alerta";
    }

    public class ActividadAdminViewModel
    {
        public string Mensaje { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Icono { get; set; } = "bi-clock-history";
    }

    // ---------- USUARIOS ----------
    public class UsuarioAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = "Usuario";
        public string Estado { get; set; } = "Activo";
        public string FechaRegistro { get; set; } = string.Empty;
        public string Foto { get; set; } = "/images/logo.jpg";
    }

    public class ListadoUsuariosAdminViewModel
    {
        public string? Busqueda { get; set; }
        public string? RolFiltro { get; set; }
        public List<UsuarioAdminViewModel> Usuarios { get; set; } = [];
    }

    public class PerfilUsuarioAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        public string? Provincia { get; set; }

        [Required(ErrorMessage = "Seleccione un rol.")]
        public string Rol { get; set; } = "Usuario";

        [Required(ErrorMessage = "Seleccione un estado.")]
        public string Estado { get; set; } = "Activo";

        public string FechaRegistro { get; set; } = string.Empty;
        public string Foto { get; set; } = "/images/logo.jpg";

        public int ComprasRealizadas { get; set; }
        public int PuntosAcumulados { get; set; }
    }

    // ---------- EMPRENDIMIENTOS ----------
    public class SolicitudEmprendimientoAdminViewModel
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string Solicitante { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string FechaSolicitud { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente";
        public string Descripcion { get; set; } = string.Empty;
        public string Logo { get; set; } = "/images/logo.jpg";
        public string? MotivoRechazo { get; set; }
    }

    public class ListadoEmprendimientosAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Pendiente";
        public List<SolicitudEmprendimientoAdminViewModel> Solicitudes { get; set; } = [];
        public int TotalPendientes { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalRechazados { get; set; }
    }

    public class RechazarSolicitudViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe escribir el motivo del rechazo.")]
        [StringLength(400, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre 10 y 400 caracteres.")]
        [Display(Name = "Motivo del rechazo")]
        public string Motivo { get; set; } = string.Empty;
    }

    // ---------- SUSCRIPCIONES ----------
    public class PlanSuscripcionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del plan es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0, 1000000, ErrorMessage = "Ingrese un precio válido.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "Seleccione la periodicidad.")]
        public string Periodicidad { get; set; } = "Mensual";

        [Display(Name = "Beneficios (uno por línea)")]
        public string Beneficios { get; set; } = string.Empty;

        public int CantidadSuscriptores { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class SuscripcionActivaViewModel
    {
        public int Id { get; set; }
        public string Emprendimiento { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaVencimiento { get; set; } = string.Empty;
        public string Estado { get; set; } = "Vigente";
    }

    public class GestionSuscripcionesViewModel
    {
        public List<PlanSuscripcionViewModel> Planes { get; set; } = [];
        public List<SuscripcionActivaViewModel> Activas { get; set; } = [];
        public int TotalPorVencer { get; set; }
    }

    // ---------- PRODUCTOS ----------
    public class ProductoAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Emprendimiento { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Estado { get; set; } = "Activo";
        public string Imagen { get; set; } = "/images/producto-1.jpg";
        public string FechaPublicacion { get; set; } = string.Empty;
        public int Reportes { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public class ListadoProductosAdminViewModel
    {
        public string? Busqueda { get; set; }
        public string? FiltroCategoria { get; set; }
        public string? FiltroEstado { get; set; }
        public List<ProductoAdminViewModel> Productos { get; set; } = [];
    }
    public class ItemVentaAdminViewModel
    {
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class VentaAdminViewModel
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Emprendimiento { get; set; } = "";
        public string Fecha { get; set; } = "";
        public decimal Total { get; set; }
        public string Estado { get; set; } = ""; // Pagada, Pendiente, Cancelada
        public string MetodoPago { get; set; } = "";
        public List<ItemVentaAdminViewModel> Items { get; set; } = [];
        public string? MotivoCancelacion { get; set; }
    }

    public class ListadoVentasAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todas";
        public List<VentaAdminViewModel> Ventas { get; set; } = [];
        public decimal TotalVendido { get; set; }
        public int TotalOrdenes { get; set; }
        public int TotalPendientes { get; set; }
        public int TotalCanceladas { get; set; }
    }
    public class ParticipanteEventoAdminViewModel
    {
        public string Nombre { get; set; } = "";
        public string Emprendimiento { get; set; } = "";
        public string Estado { get; set; } = ""; // Confirmado, Pendiente
    }
    
    public class EventoAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int CupoTotal { get; set; }
        public int Inscritos { get; set; }
        public string Estado { get; set; } = "Programado"; // Programado, Cancelado, Finalizado
        public bool QrGenerado { get; set; }
        public List<string> EmprendimientosAsignados { get; set; } = [];
        public List<ParticipanteEventoAdminViewModel> Participantes { get; set; } = [];
    }

    public class ListadoEventosAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todos";
        public List<EventoAdminViewModel> Eventos { get; set; } = [];
    }
    public class InscritoTallerAdminViewModel
    {
        public string Nombre { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Estado { get; set; } = ""; // Confirmado, Pendiente
    }

    public class TallerAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int CuposTotales { get; set; }
        public int CuposDisponibles { get; set; }
        public string Estado { get; set; } = "Programado"; // Programado, Cancelado, Finalizado
        public bool QrGenerado { get; set; }
        public List<InscritoTallerAdminViewModel> Inscritos { get; set; } = [];
    }

    public class ListadoTalleresAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todos";
        public List<TallerAdminViewModel> Talleres { get; set; } = [];
    }
    public class RecompensaAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int CostoPuntos { get; set; }
        public bool Activa { get; set; } = true;
        public int CantidadCanjes { get; set; }
    }

    public class MovimientoPuntosAdminViewModel
    {
        public string Usuario { get; set; } = "";
        public string Tipo { get; set; } = ""; // Ganados, Canjeados, Ajuste
        public int Puntos { get; set; }
        public string Motivo { get; set; } = "";
        public string Fecha { get; set; } = "";
    }

    public class PuntosRecompensasAdminViewModel
    {
        public string Pestana { get; set; } = "Recompensas"; // Recompensas, Movimientos
        public List<RecompensaAdminViewModel> Recompensas { get; set; } = [];
        public List<MovimientoPuntosAdminViewModel> Movimientos { get; set; } = [];
    }

    public class AjustarPuntosViewModel
    {
        public string Correo { get; set; } = "";
        public int Puntos { get; set; }
        public string Motivo { get; set; } = "";
    }
    public class AsistenteQrAdminViewModel
    {
        public string Nombre { get; set; } = "";
        public string Correo { get; set; } = "";
        public bool Presente { get; set; }
    }

    public class ActividadQrAdminViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = ""; // Evento, Taller
        public string Fecha { get; set; } = "";
        public bool QrGenerado { get; set; }
        public List<AsistenteQrAdminViewModel> Asistentes { get; set; } = [];
    }

}
