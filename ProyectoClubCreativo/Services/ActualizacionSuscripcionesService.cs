using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;

namespace ProyectoClubCreativo.Services
{
    public class ActualizacionSuscripcionesService
    {
        private readonly ClubCreativoDbContext _context;

        public ActualizacionSuscripcionesService(
            ClubCreativoDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarEstadosAsync()
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            // 1. Marcar como vencidas las suscripciones
            // cuyo período ya terminó.
            var vencidas = await _context.Suscripciones
                .Where(s =>
                    s.Estado == "Activa" &&
                    s.FechaFin < hoy)
                .ToListAsync();

            foreach (var suscripcion in vencidas)
            {
                suscripcion.Estado = "Vencida";
            }

            // Liberar el índice único antes de activar otros planes.
            await _context.SaveChangesAsync();

            // 2. Buscar planes programados que ya pueden activarse.
            var programadas = await _context.Suscripciones
                .Where(s =>
                    s.Estado == "Pendiente" &&
                    s.FechaInicio <= hoy &&
                    s.FechaFin >= hoy &&
                    _context.SolicitudesPagoSuscripcion.Any(p =>
                        p.IdSuscripcionGenerada == s.IdSuscripcion &&
                        p.Estado == "Aprobada"))
                .OrderBy(s => s.FechaInicio)
                .ToListAsync();

            foreach (var suscripcion in programadas)
            {
                // Evitar activar otro plan cuando ya existe
                // una suscripción activa del mismo emprendimiento.
                bool existeActiva = await _context.Suscripciones
                    .AnyAsync(s =>
                        s.IdEmprendimiento == suscripcion.IdEmprendimiento &&
                        s.IdSuscripcion != suscripcion.IdSuscripcion &&
                        s.Estado == "Activa");

                if (existeActiva)
                {
                    continue;
                }

                suscripcion.Estado = "Activa";

                // Guardar cada activación para que la siguiente
                // comprobación considere los cambios anteriores.
                await _context.SaveChangesAsync();
            }

            await transaccion.CommitAsync();
        }
    }
}