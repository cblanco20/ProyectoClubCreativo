namespace ProyectoClubCreativo.Models.ViewModels
{
    public class NoticiaPublicaViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Contenido { get; set; } = "";
        public string Resumen { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string ImagenUrl { get; set; } = "/images/noticia-comunidad.jpg";
        public DateTime Fecha { get; set; }
    }

    public class ListadoNoticiasPublicasViewModel
    {
        public NoticiaPublicaViewModel? Destacada { get; set; }
        public List<NoticiaPublicaViewModel> Recientes { get; set; } = [];
    }

    public class DetalleNoticiaPublicaViewModel
    {
        public NoticiaPublicaViewModel Noticia { get; set; } = new();
        public List<NoticiaPublicaViewModel> Relacionadas { get; set; } = [];
    }
}
