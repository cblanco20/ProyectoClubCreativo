namespace ProyectoClubCreativo.Models.ViewModels
{
    public class InicioViewModel
    {
        public List<EventoPublicoViewModel> Eventos { get; set; } = [];
        public List<TallerPublicoViewModel> Talleres { get; set; } = [];
        public List<EmprendimientoPublicoViewModel> Emprendimientos { get; set; } = [];
        public PromocionPublicaViewModel? Promocion { get; set; }
        public List<string> ImagenesComunidad { get; set; } = [];
    }
}