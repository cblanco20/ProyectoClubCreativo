namespace ProyectoClubCreativo.Models
{
    public static class ReglasTalleres
    {
        public const int UmbralUltimosCupos = 2;

        public const int HorasLimiteCancelacion = 24;

        public const string Borrador = "Borrador";
        public const string Programado = "Programado";
        public const string Cancelado = "Cancelado";
        public const string Finalizado = "Finalizado";

        public const string InscripcionConfirmada = "Confirmada";
        public const string InscripcionCancelada = "Cancelada";

        public static bool InscripcionActiva(string estado) =>
            estado == "Pendiente" || estado == InscripcionConfirmada;

        public static bool QuedanPocosCupos(int cuposDisponibles) =>
            cuposDisponibles > 0 && cuposDisponibles <= UmbralUltimosCupos;
    }
}
