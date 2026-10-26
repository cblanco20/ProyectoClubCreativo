using System;

namespace ProyectoClubCreativo.Models.Entities
{
    public class ConfiguracionPunto
    {
        public int IdConfiguracion { get; set; }

        public decimal ColonesPorPunto { get; set; }

        public DateTime FechaActualizacion { get; set; }
    }
}