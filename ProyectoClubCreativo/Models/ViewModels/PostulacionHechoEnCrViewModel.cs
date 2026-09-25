using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class PostulacionHechoEnCrViewModel
    {
        public string NombreEmprendimiento { get; set; } = string.Empty;

        // Solo los emprendimientos aprobados pueden postularse.
        public bool EstaAprobado { get; set; }

        // Indica si el emprendimiento ya fue postulado a Hecho en CR.
        public bool YaPostulado { get; set; }

        [Display(Name = "Confirmo que deseo postular mi emprendimiento a Hecho en CR")]
        public bool ConfirmaPostulacion { get; set; }
    }
}
