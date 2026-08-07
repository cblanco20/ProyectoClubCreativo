using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels.Admin
{
    // ---------- NOTIFICACIONES ----------
    public class NotificacionFormViewModel
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [Display(Name = "Mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione el tipo de destinatario.")]
        [Display(Name = "Destinatario")]
        public string TipoDestinatario { get; set; } = "Todos"; // Individual, Grupo, Todos

        [Display(Name = "Correo del usuario")]
        public string? UsuarioDestino { get; set; }

        [Display(Name = "Grupo")]
        public string? GrupoDestino { get; set; } // Usuarios, Emprendedores, Administradores
    }

    public class NotificacionEnviadaViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string Destinatario { get; set; } = "";
        public string Fecha { get; set; } = "";
        public int TotalDestinatarios { get; set; }
    }

    public class NotificacionesAdminViewModel
    {
        public string Pestana { get; set; } = "Enviar"; // Enviar, Historial
        public NotificacionFormViewModel Nueva { get; set; } = new();
        public List<NotificacionEnviadaViewModel> Historial { get; set; } = [];
    }

    // ---------- GALERÍAS ----------
    public class ImagenGaleriaViewModel
    {
        public int Id { get; set; }
        public string Url { get; set; } = "/images/logo.jpg";
        public string NombreArchivo { get; set; } = "";
    }

    public class GaleriaAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la galería es obligatorio.")]
        [Display(Name = "Nombre de la galería")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = "";

        [Required(ErrorMessage = "Seleccione una categoría.")]
        public string Categoria { get; set; } = "Eventos"; // Eventos, Talleres, Emprendimientos, General

        public string Estado { get; set; } = "Publicada"; // Publicada, Oculta
        public string Portada { get; set; } = "/images/logo.jpg";
        public string FechaCreacion { get; set; } = "";
        public List<ImagenGaleriaViewModel> Imagenes { get; set; } = [];
    }

    // ---------- NOTICIAS Y ANUNCIOS ----------
    public class NoticiaAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        public string Titulo { get; set; } = "";

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        public string Contenido { get; set; } = "";

        [Required(ErrorMessage = "Seleccione una categoría.")]
        public string Categoria { get; set; } = "Anuncio"; // Noticia, Anuncio

        public string Estado { get; set; } = "Borrador"; // Borrador, Publicada, Inactiva
        public string Imagen { get; set; } = "/images/logo.jpg";
        public string Autor { get; set; } = "Jossete Sánchez";
        public string FechaPublicacion { get; set; } = "";
    }

    public class ListadoNoticiasAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todas";
        public List<NoticiaAdminViewModel> Noticias { get; set; } = [];
    }

    // ---------- REPORTES Y ESTADÍSTICAS ----------
    public class FiltroReporteViewModel
    {
        [Display(Name = "Reporte")]
        public string TipoReporte { get; set; } = "Ventas"; // Ventas, Usuarios, Emprendimientos, Productos, Eventos

        [Display(Name = "Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicio { get; set; }

        [Display(Name = "Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Categoría")]
        public string? Categoria { get; set; }
    }

    public class FilaReporteViewModel
    {
        public string Columna1 { get; set; } = "";
        public string Columna2 { get; set; } = "";
        public string Columna3 { get; set; } = "";
        public string Columna4 { get; set; } = "";
    }

    public class ReporteAdminViewModel
    {
        public FiltroReporteViewModel Filtro { get; set; } = new();
        public List<PuntoGraficoViewModel> GraficoBarras { get; set; } = [];
        public List<PuntoGraficoViewModel> GraficoDona { get; set; } = [];
        public List<string> Columnas { get; set; } = [];
        public List<FilaReporteViewModel> Filas { get; set; } = [];
        public string Titulo { get; set; } = "";
    }

    // ---------- MODERACIÓN DE COMENTARIOS ----------
    public class ComentarioAdminViewModel
    {
        public int Id { get; set; }
        public string Autor { get; set; } = "";
        public string Contenido { get; set; } = "";
        public string Origen { get; set; } = ""; // Producto, Evento, Taller, Noticia
        public string ElementoRelacionado { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Estado { get; set; } = "Visible"; // Visible, Reportado, Eliminado
        public int Reportes { get; set; }
    }

    public class ListadoComentariosAdminViewModel
    {
        public string? Busqueda { get; set; }
        public string FiltroEstado { get; set; } = "Todos";
        public string FiltroOrigen { get; set; } = "Todos";
        public List<ComentarioAdminViewModel> Comentarios { get; set; } = [];
    }

    // ---------- CATEGORÍAS Y ETIQUETAS ----------
    public class CategoriaAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = "";

        public string Descripcion { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el tipo.")]
        public string Tipo { get; set; } = "Categoria"; // Categoria, Etiqueta

        [Required(ErrorMessage = "Seleccione el módulo.")]
        public string Modulo { get; set; } = "Productos"; // Productos, Eventos, Talleres, Noticias

        public int ElementosAsociados { get; set; }
    }

    public class ListadoCategoriasAdminViewModel
    {
        public string FiltroTipo { get; set; } = "Categoria";
        public List<CategoriaAdminViewModel> Categorias { get; set; } = [];
    }

    public class ElementoAsociadoViewModel
    {
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Estado { get; set; } = "";
    }

    public class ElementosCategoriaViewModel
    {
        public CategoriaAdminViewModel Categoria { get; set; } = new();
        public List<ElementoAsociadoViewModel> Elementos { get; set; } = [];
    }

    // ---------- PROMOCIONES Y CAMPAÑAS ----------
    public class PromocionAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = "";

        public string Descripcion { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el tipo de descuento.")]
        [Display(Name = "Tipo de descuento")]
        public string TipoDescuento { get; set; } = "Porcentaje"; // Porcentaje, Monto fijo

        [Range(0, 1000000, ErrorMessage = "Ingrese un valor válido.")]
        [Display(Name = "Valor del descuento")]
        public decimal ValorDescuento { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de fin")]
        public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(7);

        public string Estado { get; set; } = "Programada"; // Programada, Activa, Finalizada, Desactivada

        [Display(Name = "Emprendimiento")]
        public string Emprendimiento { get; set; } = "Todos";

        public int Usos { get; set; }
    }

    public class ListadoPromocionesAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todas";
        public List<PromocionAdminViewModel> Promociones { get; set; } = [];
    }

    // ---------- ENCUESTAS ----------
    public class OpcionPreguntaViewModel
    {
        public string Texto { get; set; } = "";
        public int Votos { get; set; }
    }

    public class PreguntaEncuestaViewModel
    {
        public string Texto { get; set; } = "";
        public List<OpcionPreguntaViewModel> Opciones { get; set; } = [];
    }

    public class EncuestaAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        public string Titulo { get; set; } = "";

        public string Descripcion { get; set; } = "";

        [Required(ErrorMessage = "Agregue al menos una pregunta.")]
        [Display(Name = "Preguntas (una por línea)")]
        public string PreguntasTexto { get; set; } = "";

        public string Estado { get; set; } = "Borrador"; // Borrador, Publicada, Cerrada
        public string FechaCreacion { get; set; } = "";
        public int TotalRespuestas { get; set; }
        public List<PreguntaEncuestaViewModel> Preguntas { get; set; } = [];
    }

    public class ListadoEncuestasAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Todas";
        public List<EncuestaAdminViewModel> Encuestas { get; set; } = [];
    }

    // ---------- SEGURIDAD Y AUDITORÍA ----------
    public class MovimientoBitacoraViewModel
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Modulo { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string DireccionIp { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string Nivel { get; set; } = "informativa"; // informativa, advertencia, critica
    }

    public class FiltroBitacoraViewModel
    {
        [Display(Name = "Usuario")]
        public string? Usuario { get; set; }

        [Display(Name = "Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicio { get; set; }

        [Display(Name = "Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Acción")]
        public string? Accion { get; set; }
    }

    public class IntentoFallidoViewModel
    {
        public string Correo { get; set; } = "";
        public string DireccionIp { get; set; } = "";
        public string Fecha { get; set; } = "";
        public int Intentos { get; set; }
        public bool Bloqueado { get; set; }
    }

    public class BitacoraAdminViewModel
    {
        public FiltroBitacoraViewModel Filtro { get; set; } = new();
        public List<MovimientoBitacoraViewModel> Movimientos { get; set; } = [];
        public List<AlertaAdminViewModel> Alertas { get; set; } = [];
        public List<IntentoFallidoViewModel> IntentosFallidos { get; set; } = [];
    }
}
