using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Services
{
    public class AuditoriaService
    {
        private readonly ClubCreativoDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(
            ClubCreativoDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Agregar(
     string modulo,
     string accion,
     string? entidad = null,
     string? idRegistro = null,
     string? descripcion = null,
     string nivel = "Informativo",
     int? idUsuario = null,
     bool delSistema = false)
        {
            HttpContext? http = _httpContextAccessor.HttpContext;

            _context.Auditoria.Add(new Auditorium
            {
                IdUsuario = delSistema
                    ? null
                    : idUsuario ?? http?.Session.GetInt32("IdUsuario"),
                Modulo = Limitar(modulo, 80)!,
                Accion = Limitar(accion, 80)!,
                Entidad = Limitar(entidad, 80),
                IdRegistro = Limitar(idRegistro, 100),
                Descripcion = Limitar(descripcion, 1500),
                DireccionIp = http?.Connection.RemoteIpAddress?.ToString(),
                Nivel = nivel,
                FechaHora = DateTime.Now
            });
        }

        public async Task RegistrarAsync(
            string modulo,
            string accion,
            string? entidad = null,
            string? idRegistro = null,
            string? descripcion = null,
            string nivel = "Informativo",
            int? idUsuario = null)
        {
            Agregar(modulo, accion, entidad, idRegistro, descripcion, nivel, idUsuario);

            await _context.SaveChangesAsync();
        }
        public async Task RegistrarAlertaAsync(
    string accion,
    string descripcion,
    string nivel = "Critico",
    int? idUsuario = null)
        {
            Agregar("Seguridad", accion, null, null, descripcion, nivel, idUsuario);

            List<int> administradores = await _context.UsuarioRoles
                .Where(ur =>
                    ur.IdRolNavigation.Nombre == "Administrador" &&
                    ur.IdRolNavigation.Activo &&
                    ur.IdUsuarioNavigation.Estado == "Activo")
                .Select(ur => ur.IdUsuario)
                .ToListAsync();

            foreach (int idAdministrador in administradores)
            {
                _context.Notificaciones.Add(new Notificacione
                {
                    IdUsuario = idAdministrador,
                    Titulo = "Alerta de seguridad",
                    Mensaje = descripcion,
                    Tipo = "Seguridad",
                    FechaEnvio = DateTime.Now,
                    Leida = false
                });
            }

            await _context.SaveChangesAsync();
        }
        private static string? Limitar(string? texto, int maximo)
        {
            if (texto is null || texto.Length <= maximo)
            {
                return texto;
            }

            return texto[..maximo];
        }
    }
}
