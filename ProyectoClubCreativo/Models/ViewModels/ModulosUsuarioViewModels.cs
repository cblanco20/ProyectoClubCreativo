using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class CompraUsuarioViewModel
    {
        public string NumeroOrden { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string MetodoEntrega { get; set; } = string.Empty;
    }

    public class DetalleCompraUsuarioViewModel
    {
        public string NumeroOrden { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string MetodoEntrega { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }

        public List<ProductoCompraViewModel> Productos { get; set; } = [];
    }

    public class ProductoCompraViewModel
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Emprendimiento { get; set; } = string.Empty;
        public string Imagen { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }

        public decimal Subtotal => Precio * Cantidad;
    }

    public class CarritoUsuarioViewModel
    {
        public List<ProductoCompraViewModel> Productos { get; set; } = [];

        public decimal Subtotal =>
            Productos.Sum(producto => producto.Subtotal);

        public decimal Descuento { get; set; }

        public decimal Total => Subtotal - Descuento;
    }

    public class ProcesoCompraViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(
            @"^[0-9]{8}$",
            ErrorMessage = "Ingrese un teléfono de 8 dígitos."
        )]
        [Display(Name = "Número de teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione una modalidad de entrega.")]
        [Display(Name = "Modalidad de entrega")]
        public string TipoEntrega { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Dirección de entrega")]
        public string? Direccion { get; set; }

        [Required(ErrorMessage = "Seleccione un método de pago.")]
        [Display(Name = "Método de pago")]
        public string MetodoPago { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
    }

    public class InscripcionUsuarioViewModel
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Imagen { get; set; } = string.Empty;
    }

    public class MovimientoPuntosViewModel
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Puntos { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public class RecompensaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int CostoPuntos { get; set; }
        public string Icono { get; set; } = string.Empty;
        public bool Disponible { get; set; }
    }

    public class PuntosRecompensasViewModel
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public int SaldoPuntos { get; set; }
        public string CodigoCliente { get; set; } = string.Empty;

        public List<MovimientoPuntosViewModel> Movimientos { get; set; } = [];
        public List<RecompensaViewModel> Recompensas { get; set; } = [];
    }

    public class FavoritoUsuarioViewModel
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Imagen { get; set; } = string.Empty;
    }

    public class ComentarioUsuarioViewModel
    {
        public int Id { get; set; }
        public string Elemento { get; set; } = string.Empty;
        public string TipoElemento { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        public int Valoracion { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class EncuestaUsuarioViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ElementoRelacionado { get; set; } = string.Empty;
        public string TipoElemento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaLimite { get; set; } = string.Empty;
    }

    public class ResponderEncuestaViewModel
    {
        public int IdEncuesta { get; set; }

        public string NombreEncuesta { get; set; } = string.Empty;

        public string ElementoRelacionado { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione un nivel de satisfacción.")]
        [Range(1, 5, ErrorMessage = "Seleccione una valoración entre 1 y 5.")]
        public int? Satisfaccion { get; set; }

        [Required(ErrorMessage = "Indique si recomendaría la actividad.")]
        public string Recomendaria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione el aspecto que más le gustó.")]
        public string AspectoFavorito { get; set; } = string.Empty;

        [StringLength(
            600,
            ErrorMessage = "El comentario no puede superar los 600 caracteres."
        )]
        public string? Comentario { get; set; }
    }

    public class NotificacionListadoViewModel
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public bool Leida { get; set; }
    }

    public class PreferenciasNotificacionViewModel
    {
        [Display(Name = "Compras y pedidos")]
        public bool Compras { get; set; }

        [Display(Name = "Eventos")]
        public bool Eventos { get; set; }

        [Display(Name = "Talleres")]
        public bool Talleres { get; set; }

        [Display(Name = "Promociones")]
        public bool Promociones { get; set; }

        [Display(Name = "Recordatorios")]
        public bool Recordatorios { get; set; }

        [Display(Name = "Correo electrónico")]
        public bool CanalCorreo { get; set; }

        [Display(Name = "Notificaciones en la plataforma")]
        public bool CanalPlataforma { get; set; }

        [Display(Name = "Frecuencia de correos")]
        [Required(ErrorMessage = "Seleccione una frecuencia.")]
        public string FrecuenciaCorreo { get; set; } = string.Empty;
    }
}
