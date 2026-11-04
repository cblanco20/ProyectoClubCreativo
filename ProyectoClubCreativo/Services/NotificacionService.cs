using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Services
{
    // HU-49: Aplica las preferencias de notificación del usuario.
    public class NotificacionService
    {
        private readonly ClubCreativoDbContext _context;
        private readonly CorreoService _correoService;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(
            ClubCreativoDbContext context,
            CorreoService correoService,
            ILogger<NotificacionService> logger)
        {
            _context = context;
            _correoService = correoService;
            _logger = logger;
        }

        // Agrega la notificación al contexto según las preferencias.
        // El llamador sigue haciendo SaveChangesAsync.
        public async Task AgregarAsync(
            Notificacione notificacion,
            bool esRecordatorio = false)
        {
            PreferenciasNotificacion? preferencias = await _context.PreferenciasNotificacions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdUsuario == notificacion.IdUsuario);

            if (preferencias is null)
            {
                _context.Notificaciones.Add(notificacion);
                return;
            }

            if (!CategoriaActiva(preferencias, notificacion.Tipo, esRecordatorio))
            {
                return;
            }

            if (preferencias.CanalPlataforma)
            {
                _context.Notificaciones.Add(notificacion);
            }

            if (preferencias.CanalCorreo &&
                preferencias.FrecuenciaCorreo == "Inmediata")
            {
                await EnviarCorreoAsync(notificacion);
            }
        }

        private static bool CategoriaActiva(
            PreferenciasNotificacion preferencias,
            string tipo,
            bool esRecordatorio)
        {
            if (esRecordatorio)
            {
                return preferencias.Recordatorios;
            }

            return tipo switch
            {
                "Compra" => preferencias.Compras,
                "Evento" => preferencias.Eventos,
                "Taller" => preferencias.Talleres,
                "Promoción" => preferencias.Promociones,
                _ => true
            };
        }

        private async Task EnviarCorreoAsync(Notificacione notificacion)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.IdUsuario == notificacion.IdUsuario)
                    .Select(u => new { u.Correo, u.Nombre })
                    .FirstOrDefaultAsync();

                if (usuario is null)
                {
                    return;
                }

                string contenido = PlantillaCorreo.Construir(
                    titulo: notificacion.Titulo,
                    cuerpoHtml:
                        $"<p>Hola <strong>{PlantillaCorreo.E(usuario.Nombre)}</strong>,</p>" +
                        $"<p>{PlantillaCorreo.E(notificacion.Mensaje)}</p>");

                await _correoService.EnviarCorreoAsync(
                    usuario.Correo,
                    notificacion.Titulo,
                    contenido);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No fue posible enviar el correo de la notificación.");
            }
        }
    }
}
