using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;
using System.Security.Cryptography;
using System.Text;
using ProyectoClubCreativo.Services;

namespace ProyectoClubCreativo.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ClubCreativoDbContext _context;
        private readonly CorreoService _correoService;

        public CuentaController(
            ClubCreativoDbContext context,
            CorreoService correoService)
        {
            _context = context;
            _correoService = correoService;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View(new InicioSesionViewModel());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(
    InicioSesionViewModel modelo
)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string correoNormalizado = modelo.Correo
                .Trim()
                .ToLowerInvariant();

            Usuario? usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.IdRolNavigation)
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correoNormalizado
                );

            if (usuario is null ||
                usuario.Estado != "Activo" ||
                !BCrypt.Net.BCrypt.Verify(
                    modelo.Contrasena,
                    usuario.ContrasenaHash
                ))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Correo o contraseña incorrectos."
                );

                return View(modelo);
            }

            Role? rol = usuario.UsuarioRoles
                .Select(ur => ur.IdRolNavigation)
                .FirstOrDefault(r => r.Activo);

            if (rol is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El usuario no tiene un rol activo asignado."
                );

                return View(modelo);
            }

            HttpContext.Session.SetInt32(
                "IdUsuario",
                usuario.IdUsuario
            );

            HttpContext.Session.SetString(
                "NombreUsuario",
                usuario.Nombre
            );

            HttpContext.Session.SetString(
                "CorreoUsuario",
                usuario.Correo
            );

            HttpContext.Session.SetString(
                "RolUsuario",
                rol.Nombre
            );

            usuario.UltimoAcceso = DateTime.Now;

            await _context.SaveChangesAsync();

            switch (rol.Nombre)
            {
                case "Administrador":
                    return RedirectToAction(
                        "Panel",
                        "Admin"
                    );

                case "Emprendedor":
                    return RedirectToAction(
                        "Panel",
                        "Emprendedor"
                    );

                case "Usuario":
                    return RedirectToAction(
                        "Panel",
                        "Usuario"
                    );

                default:
                    HttpContext.Session.Clear();

                    ModelState.AddModelError(
                        string.Empty,
                        "El rol asignado al usuario no es válido."
                    );

                    return View(modelo);
            }
        }

        [HttpGet]
        public IActionResult SeleccionarRegistro()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RegistroUsuario()
        {
            return View(new RegistroUsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroUsuario(
    RegistroUsuarioViewModel modelo
)
        {
            if (!modelo.AceptaTerminos)
            {
                ModelState.AddModelError(
                    nameof(modelo.AceptaTerminos),
                    "Debe aceptar los términos y condiciones."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(modelo);
            }


            string correoNormalizado = modelo.Correo
                .Trim()
                .ToLowerInvariant();


            bool correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo.ToLower() == correoNormalizado);

            if (correoExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "Ya existe una cuenta registrada con este correo electrónico."
                );

                return View(modelo);
            }


            bool provinciaExiste = await _context.Provincias
                .AnyAsync(p => p.IdProvincia == modelo.IdProvincia);

            if (!provinciaExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdProvincia),
                    "La provincia seleccionada no es válida."
                );

                return View(modelo);
            }


            string[] apellidos = modelo.Apellidos
                .Trim()
                .Split(
                    ' ',
                    2,
                    StringSplitOptions.RemoveEmptyEntries
                );

            string apellidoPaterno = apellidos[0];

            string? apellidoMaterno =
                apellidos.Length > 1
                    ? apellidos[1]
                    : null;


            string contrasenaHash =
                BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena);


            Usuario usuario = new()
            {
                Nombre = modelo.Nombre.Trim(),
                ApellidoPaterno = apellidoPaterno,
                ApellidoMaterno = apellidoMaterno,
                Correo = correoNormalizado,
                Telefono = modelo.Telefono.Trim(),
                IdProvincia = modelo.IdProvincia,
                FechaNacimiento = modelo.FechaNacimiento.HasValue
                    ? DateOnly.FromDateTime(modelo.FechaNacimiento.Value)
                    : null,
                ContrasenaHash = contrasenaHash,
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };


            await _context.Usuarios.AddAsync(usuario);

            await _context.SaveChangesAsync();


            Role? rolUsuario = await _context.Roles
                .FirstOrDefaultAsync(
                    r => r.Nombre == "Usuario" && r.Activo
                );

            if (rolUsuario is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se encontró el rol de usuario en el sistema."
                );

                return View(modelo);
            }


            UsuarioRole usuarioRol = new()
            {
                IdUsuario = usuario.IdUsuario,
                IdRol = rolUsuario.IdRol,
                FechaAsignacion = DateTime.Now
            };


            await _context.UsuarioRoles.AddAsync(usuarioRol);

            await _context.SaveChangesAsync();


            TempData["MensajePanel"] =
                $"¡Bienvenida, {usuario.Nombre}! Tu cuenta fue creada correctamente.";


            return RedirectToAction(
                "Panel",
                "Usuario"
            );
        }

        [HttpGet]
        public async Task<IActionResult> RegistroEmprendedor()
        {
            RegistroEmprendedorViewModel modelo = new();

            await CargarDatosRegistroEmprendedorAsync(modelo);

            return View(modelo);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroEmprendedor(
            RegistroEmprendedorViewModel modelo)
        {
            if (!modelo.AceptaTerminos)
            {
                ModelState.AddModelError(
                    nameof(modelo.AceptaTerminos),
                    "Debe aceptar los términos y condiciones de participación."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosRegistroEmprendedorAsync(modelo);
                return View(modelo);
            }

            string correoNormalizado = modelo.Correo
                .Trim()
                .ToLowerInvariant();

            bool correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Correo.ToLower() == correoNormalizado
                );

            if (correoExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "Ya existe una cuenta registrada con este correo electrónico."
                );

                await CargarDatosRegistroEmprendedorAsync(modelo);

                return View(modelo);
            }

            bool provinciaExiste = await _context.Provincias
                .AnyAsync(p =>
                    p.IdProvincia == modelo.IdProvincia
                );

            if (!provinciaExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdProvincia),
                    "La provincia seleccionada no es válida."
                );

                await CargarDatosRegistroEmprendedorAsync(modelo);

                return View(modelo);
            }

            bool categoriaExiste = await _context.Categorias
                .AnyAsync(c =>
                    c.IdCategoria == modelo.IdCategoria &&
                    c.Modulo == "Emprendimientos" &&
                    c.Activa
                );

            if (!categoriaExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "La categoría seleccionada no es válida."
                );

                await CargarDatosRegistroEmprendedorAsync(modelo);

                return View(modelo);
            }

            Role? rolEmprendedor = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Nombre == "Emprendedor" &&
                    r.Activo
                );

            if (rolEmprendedor is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se encontró el rol de emprendedor en el sistema."
                );

                await CargarDatosRegistroEmprendedorAsync(modelo);

                return View(modelo);
            }

            string[] apellidos = modelo.Apellidos
                .Trim()
                .Split(
                    ' ',
                    2,
                    StringSplitOptions.RemoveEmptyEntries
                );

            string apellidoPaterno = apellidos[0];

            string? apellidoMaterno =
                apellidos.Length > 1
                    ? apellidos[1]
                    : null;

            string contrasenaHash =
                BCrypt.Net.BCrypt.HashPassword(
                    modelo.Contrasena
                );

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Usuario usuario = new()
                {
                    Nombre = modelo.Nombre.Trim(),
                    ApellidoPaterno = apellidoPaterno,
                    ApellidoMaterno = apellidoMaterno,
                    Correo = correoNormalizado,
                    Telefono = modelo.Telefono.Trim(),
                    IdProvincia = modelo.IdProvincia,
                    FechaNacimiento = null,
                    ContrasenaHash = contrasenaHash,
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now
                };

                await _context.Usuarios.AddAsync(usuario);
                await _context.SaveChangesAsync();

                UsuarioRole usuarioRol = new()
                {
                    IdUsuario = usuario.IdUsuario,
                    IdRol = rolEmprendedor.IdRol,
                    FechaAsignacion = DateTime.Now
                };

                await _context.UsuarioRoles.AddAsync(usuarioRol);
                await _context.SaveChangesAsync();

                Emprendimiento emprendimiento = new()
                {
                    IdUsuarioPropietario = usuario.IdUsuario,
                    IdCategoria = modelo.IdCategoria,
                    NombreComercial = modelo.NombreEmprendimiento.Trim(),
                    Descripcion = modelo.Descripcion.Trim(),
                    Telefono = modelo.Telefono.Trim(),
                    Correo = correoNormalizado,
                    SitioWeb = string.IsNullOrWhiteSpace(modelo.SitioWeb)
                        ? null
                        : modelo.SitioWeb.Trim(),
                    ParticipaClubCreativo = true,
                    ParticipaHechoEnCr = false,
                    EstadoAprobacion = "Pendiente",
                    Activo = true
                };

                await _context.Emprendimientos.AddAsync(emprendimiento);
                await _context.SaveChangesAsync();

                EmprendimientoRevisione revision = new()
                {
                    IdEmprendimiento = emprendimiento.IdEmprendimiento,
                    FechaSolicitud = DateTime.Now,
                    FechaResolucion = null
                };

                await _context.EmprendimientoRevisiones.AddAsync(revision);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                HttpContext.Session.SetInt32(
                    "IdUsuario",
                    usuario.IdUsuario
                );

                HttpContext.Session.SetString(
                    "NombreUsuario",
                    usuario.Nombre
                );

                HttpContext.Session.SetString(
                    "CorreoUsuario",
                    usuario.Correo
                );

                HttpContext.Session.SetString(
                    "RolUsuario",
                    rolEmprendedor.Nombre
                );


                TempData["MensajeSolicitud"] =
                    "Tu cuenta fue creada correctamente y tu solicitud de emprendimiento ya fue enviada. Te notificaremos por correo cuando sea resuelta.";

                return RedirectToAction(
                    "EstadoSolicitud",
                    "Emprendedor"
                );
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al crear la cuenta. Intente nuevamente."
                );

                await CargarDatosRegistroEmprendedorAsync(modelo);

                return View(modelo);
            }
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            return View(new RecuperarContrasenaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(
    RecuperarContrasenaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string correoNormalizado = modelo.Correo
                .Trim()
                .ToLowerInvariant();

            Usuario? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correoNormalizado);

            if (usuario is not null)
            {
                List<TokensRecuperacionContrasena> tokensAnteriores =
                    await _context.TokensRecuperacionContrasenas
                        .Where(t =>
                            t.IdUsuario == usuario.IdUsuario &&
                            !t.Utilizado)
                        .ToListAsync();

                foreach (TokensRecuperacionContrasena tokenAnterior
                         in tokensAnteriores)
                {
                    tokenAnterior.Utilizado = true;
                    tokenAnterior.FechaUso = DateTime.Now;
                }

                string token = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32)
                );

                string tokenHash;

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(token)
                    );

                    tokenHash = Convert.ToHexString(hashBytes);
                }

                TokensRecuperacionContrasena nuevoToken = new()
                {
                    IdUsuario = usuario.IdUsuario,
                    TokenHash = tokenHash,
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddMinutes(30),
                    Utilizado = false
                };

                await _context.TokensRecuperacionContrasenas
                    .AddAsync(nuevoToken);

                await _context.SaveChangesAsync();

                string enlaceRecuperacion = Url.Action(
                    "RestablecerContrasena",
                    "Cuenta",
                    new { token },
                    Request.Scheme
                )!;

                string asunto =
                    "Recuperación de contraseña - Club Creativo";

                string contenidoCorreo = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
</head>

<body style=""
    margin: 0;
    padding: 0;
    background-color: #f8f5fb;
    font-family: Arial, Helvetica, sans-serif;
    color: #33283f;
"">

    <table width=""100%"" cellpadding=""0"" cellspacing=""0""
           style=""background-color: #f8f5fb; padding: 40px 15px;"">
        <tr>
            <td align=""center"">

                <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                       style=""
                           max-width: 600px;
                           background-color: #ffffff;
                           border-radius: 16px;
                           overflow: hidden;
                           box-shadow: 0 4px 18px rgba(51, 40, 63, 0.10);
                       "">

                    <!-- Encabezado -->
                    <tr>
                        <td align=""center""
                            style=""
                                background-color: #6f42c1;
                                padding: 30px 20px;
                            "">

                            <div style=""
                                font-size: 28px;
                                font-weight: bold;
                                color: #ffffff;
                            "">
                                Club Creativo
                            </div>

                            <div style=""
                                margin-top: 7px;
                                font-size: 14px;
                                color: #eee7ff;
                            "">
                                Comunidad creativa MiVo
                            </div>

                        </td>
                    </tr>

                    <!-- Contenido -->
                    <tr>
                        <td style=""padding: 35px 40px;"">

                            <h2 style=""
                                margin-top: 0;
                                margin-bottom: 20px;
                                color: #33283f;
                                font-size: 24px;
                            "">
                                Recuperación de contraseña
                            </h2>

                            <p style=""
                                font-size: 16px;
                                line-height: 1.6;
                                margin-bottom: 15px;
                            "">
                                Hola <strong>{usuario.Nombre}</strong>,
                            </p>

                            <p style=""
                                font-size: 16px;
                                line-height: 1.6;
                                color: #655c6b;
                            "">
                                Recibimos una solicitud para restablecer
                                la contraseña de tu cuenta en Club Creativo.
                            </p>

                            <p style=""
                                font-size: 16px;
                                line-height: 1.6;
                                color: #655c6b;
                            "">
                                Presiona el siguiente botón para crear
                                una nueva contraseña:
                            </p>

                            <!-- Botón -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                                   style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">

                                        <a href=""{enlaceRecuperacion}""
                                           style=""
                                               display: inline-block;
                                               background-color: #6f42c1;
                                               color: #ffffff;
                                               text-decoration: none;
                                               padding: 14px 28px;
                                               border-radius: 8px;
                                               font-size: 16px;
                                               font-weight: bold;
                                           "">
                                            Restablecer mi contraseña
                                        </a>

                                    </td>
                                </tr>
                            </table>

                            <!-- Aviso -->
                            <div style=""
                                background-color: #faf7ff;
                                border-left: 4px solid #a855c7;
                                padding: 15px 18px;
                                border-radius: 6px;
                                margin-bottom: 25px;
                            "">
                                <p style=""
                                    margin: 0;
                                    color: #655c6b;
                                    font-size: 14px;
                                    line-height: 1.5;
                                "">
                                    Este enlace estará disponible durante
                                    <strong>30 minutos</strong>.
                                </p>
                            </div>

                            <p style=""
                                font-size: 14px;
                                line-height: 1.6;
                                color: #706779;
                                margin-bottom: 0;
                            "">
                                Si no solicitaste este cambio, puedes ignorar
                                este correo. Tu contraseña actual seguirá
                                funcionando normalmente.
                            </p>

                        </td>
                    </tr>

                    <!-- Pie -->
                    <tr>
                        <td align=""center""
                            style=""
                                background-color: #faf7ff;
                                padding: 22px;
                                border-top: 1px solid #eee8f1;
                            "">

                            <p style=""
                                margin: 0 0 5px 0;
                                color: #6f42c1;
                                font-size: 14px;
                                font-weight: bold;
                            "">
                                Club Creativo MiVo
                            </p>

                            <p style=""
                                margin: 0;
                                color: #a097a6;
                                font-size: 12px;
                            "">
                                Impulsando la creatividad y el emprendimiento
                            </p>

                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>
";

                try
                {
                    await _correoService.EnviarCorreoAsync(
                        usuario.Correo,
                        asunto,
                        contenidoCorreo
                    );
                }
                catch
                {
                       
                }
            }

            TempData["MensajeExito"] =
                "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.";

            return RedirectToAction(nameof(RecuperarContrasena));
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["MensajeError"] =
                    "El enlace de recuperación no es válido.";

                return RedirectToAction(nameof(RecuperarContrasena));
            }

            string tokenHash;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(token)
                );

                tokenHash = Convert.ToHexString(hashBytes);
            }

            TokensRecuperacionContrasena? tokenRecuperacion =
                await _context.TokensRecuperacionContrasenas
                    .FirstOrDefaultAsync(t =>
                        t.TokenHash == tokenHash);

            if (tokenRecuperacion == null ||
                tokenRecuperacion.Utilizado ||
                tokenRecuperacion.FechaExpiracion < DateTime.Now)
            {
                TempData["MensajeError"] =
                    "El enlace de recuperación no es válido o ha expirado.";

                return RedirectToAction(nameof(RecuperarContrasena));
            }

            RestablecerContrasenaViewModel modelo = new()
            {
                Token = token
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(
    RestablecerContrasenaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string tokenHash;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(modelo.Token)
                );

                tokenHash = Convert.ToHexString(hashBytes);
            }

            TokensRecuperacionContrasena? tokenRecuperacion =
                await _context.TokensRecuperacionContrasenas
                    .FirstOrDefaultAsync(t =>
                        t.TokenHash == tokenHash);

            if (tokenRecuperacion == null ||
                tokenRecuperacion.Utilizado ||
                tokenRecuperacion.FechaExpiracion < DateTime.Now)
            {
                TempData["MensajeError"] =
                    "El enlace de recuperación no es válido o ha expirado.";

                return RedirectToAction(nameof(RecuperarContrasena));
            }

            Usuario? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == tokenRecuperacion.IdUsuario);

            if (usuario == null)
            {
                TempData["MensajeError"] =
                    "No fue posible restablecer la contraseña.";

                return RedirectToAction(nameof(RecuperarContrasena));
            }

            usuario.ContrasenaHash =
                BCrypt.Net.BCrypt.HashPassword(modelo.NuevaContrasena);

            tokenRecuperacion.Utilizado = true;
            tokenRecuperacion.FechaUso = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                "Tu contraseña se actualizó correctamente. Ya puedes iniciar sesión.";

            return RedirectToAction("IniciarSesion");
        }

        private async Task CargarDatosRegistroEmprendedorAsync(
    RegistroEmprendedorViewModel modelo
)
        {
            modelo.Provincias = await _context.Provincias
                .OrderBy(p => p.Nombre)
                .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = p.IdProvincia.ToString(),
                    Text = p.Nombre
                })
                .ToListAsync();


            modelo.Categorias = await _context.Categorias
                .Where(c =>
                    c.Modulo == "Emprendimientos" &&
                    c.Activa
                )
                .OrderBy(c => c.Nombre)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.IdCategoria.ToString(),
                    Text = c.Nombre
                })
                .ToListAsync();
        }
    }
}
