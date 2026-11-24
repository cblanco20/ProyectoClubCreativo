using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class FiltroCatalogoViewModel
    {
        public string Accion { get; set; } = string.Empty;
        public int? Categoria { get; set; }
        public int? Etiqueta { get; set; }
        public bool MostrarCategorias { get; set; } = true;
        public List<Categoria> Categorias { get; set; } = [];
        public List<Etiqueta> Etiquetas { get; set; } = [];
        public Dictionary<string, string> RutaExtra { get; set; } = [];
    }

    public class PromocionPublicaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Descuento { get; set; } = string.Empty;
        public string AplicaA { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int? UsosRestantes { get; set; }
    }
}
