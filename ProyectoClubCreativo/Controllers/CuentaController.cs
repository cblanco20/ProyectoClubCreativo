using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ClubCreativoDbContext _context;

        public CuentaController(ClubCreativoDbContext context)
        {
            _context = context;
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

            // Guardar los datos principales del usuario en sesión
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

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.Now;

            await _context.SaveChangesAsync();

            // Redirigir según el rol
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
    RegistroEmprendedorViewModel modelo
)
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
                .AnyAsync(u => u.Correo.ToLower() == correoNormalizado);

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
                .AnyAsync(p => p.IdProvincia == modelo.IdProvincia);

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
                .FirstOrDefaultAsync(
                    r => r.Nombre == "Emprendedor" && r.Activo
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
                BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena);


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
                    ParticipaClubCreativo = false,
                    ParticipaHechoEnCr = false,
                    EstadoAprobacion = "Pendiente",
                    Activo = true
                };


                await _context.Emprendimientos.AddAsync(emprendimiento);

                await _context.SaveChangesAsync();


                EmprendimientoRevisione revision = new()
                {
                    IdEmprendimiento = emprendimiento.IdEmprendimiento,
                    FechaSolicitud = DateTime.Now
                };


                await _context.EmprendimientoRevisiones.AddAsync(revision);

                await _context.SaveChangesAsync();


                await transaccion.CommitAsync();


                TempData["MensajeEmprendedor"] =
                    $"¡Bienvenido, {usuario.Nombre}! Tu solicitud de emprendimiento fue registrada correctamente.";


                return RedirectToAction(
                    "Panel",
                    "Emprendedor"
                );
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al registrar el emprendimiento. Intente nuevamente."
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
        public IActionResult RecuperarContrasena(
            RecuperarContrasenaViewModel modelo
        )
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeExito"] =
                "Se enviaría un enlace de recuperación al correo indicado.";

            return RedirectToAction(nameof(RecuperarContrasena));
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
