using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Controllers
{
    public class EmprendedorController : Controller
    {
        private readonly ClubCreativoDbContext _context;

        public EmprendedorController(
            ClubCreativoDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            if (idUsuario is null || rolUsuario != "Emprendedor")
            {
                context.Result = RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );

                return;
            }

            base.OnActionExecuting(context);
        }

        public IActionResult Panel()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> SolicitudEmprendimiento(
    int? idCategoria)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo"
                );

            if (usuario == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            SolicitudEmprendimientoViewModel modelo = new()
            {
                NombreComercial = string.Empty,
                Descripcion = string.Empty,

                IdCategoria = idCategoria,

                Cedula = string.Empty,

                Telefono =
                    usuario.Telefono ?? string.Empty,

                Correo = usuario.Correo,

                SitioWeb = null,
                Instagram = null,
                Facebook = null,

                ParticipaClubCreativo = false,
                ParticipaHechoEnCr = false,

                InformacionParticipacion = string.Empty,
                ConfirmaInformacion = false
            };

            modelo.Categorias = await _context.Categorias
                .Where(c =>
                    c.Modulo == "Emprendimientos" &&
                    c.Activa
                )
                .OrderBy(c => c.Nombre)
                .Select(c =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.IdCategoria.ToString(),
                        Text = c.Nombre
                    })
                .ToListAsync();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitudEmprendimiento(
            SolicitudEmprendimientoViewModel modelo)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            // El usuario debe confirmar que la información
            // suministrada es correcta.
            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información suministrada es correcta."
                );
            }

            // Debe seleccionar al menos una opción de participación.
            if (!modelo.ParticipaClubCreativo &&
                !modelo.ParticipaHechoEnCr)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar al menos una opción de participación."
                );
            }

            // Comprobar que la categoría realmente existe.
            if (modelo.IdCategoria.HasValue)
            {
                bool categoriaExiste =
                    await _context.Categorias.AnyAsync(c =>
                        c.IdCategoria == modelo.IdCategoria.Value &&
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    );

                if (!categoriaExiste)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "La categoría seleccionada no es válida."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                return View(modelo);
            }

            Usuario? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo"
                );

            if (usuario == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            // Evita crear dos emprendimientos para
            // el mismo propietario desde este proceso.
            bool yaTieneEmprendimiento =
                await _context.Emprendimientos.AnyAsync(e =>
                    e.IdUsuarioPropietario == usuario.IdUsuario
                );

            if (yaTieneEmprendimiento)
            {
                TempData["MensajeError"] =
                    "Ya existe una solicitud de emprendimiento asociada a tu cuenta.";

                return RedirectToAction(
     "EstadoSolicitud",
     "Emprendedor"
 );
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Emprendimiento emprendimiento = new()
                {
                    IdUsuarioPropietario = usuario.IdUsuario,
                    IdCategoria = modelo.IdCategoria,

                    NombreComercial =
                        modelo.NombreComercial.Trim(),

                    Descripcion =
                        modelo.Descripcion.Trim(),

                    CedulaJuridica =
                        modelo.Cedula.Trim(),

                    Telefono =
                        modelo.Telefono.Trim(),

                    Correo =
                        modelo.Correo.Trim().ToLowerInvariant(),

                    SitioWeb =
                        string.IsNullOrWhiteSpace(modelo.SitioWeb)
                            ? null
                            : modelo.SitioWeb.Trim(),

                    Instagram =
                        string.IsNullOrWhiteSpace(modelo.Instagram)
                            ? null
                            : modelo.Instagram.Trim(),

                    Facebook =
                        string.IsNullOrWhiteSpace(modelo.Facebook)
                            ? null
                            : modelo.Facebook.Trim(),

                    LogoUrl = null,

                    ParticipaClubCreativo =
                        modelo.ParticipaClubCreativo,

                    ParticipaHechoEnCr =
                        modelo.ParticipaHechoEnCr,

                    EstadoAprobacion = "Pendiente",
                    Activo = true
                };

                await _context.Emprendimientos
                    .AddAsync(emprendimiento);

                await _context.SaveChangesAsync();

                EmprendimientoRevisione revision = new()
                {
                    IdEmprendimiento =
                        emprendimiento.IdEmprendimiento,

                    FechaSolicitud = DateTime.Now,
                    FechaResolucion = null
                };

                await _context.EmprendimientoRevisiones
                    .AddAsync(revision);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                TempData["MensajeSolicitud"] =
                    "Tu solicitud de emprendimiento fue enviada correctamente y se encuentra pendiente de revisión.";

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
                    "Ocurrió un error al enviar la solicitud. Intente nuevamente."
                );

                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                return View(modelo);
            }
        }

        [HttpGet]
        public IActionResult EstadoSolicitud()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PerfilEmprendimiento()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            return View();
        }

        [HttpGet]
        public IActionResult EditarEmprendimiento()
        {
            var modelo = new SolicitudEmprendimientoViewModel
            {
                NombreComercial = "Artesanías MiVo",
                Descripcion =
                    "Elaboramos productos artesanales hechos a mano para decoración, regalos y pedidos personalizados.",
                Categoria = "Arte e ilustración",
                Cedula = "1-1234-5678",
                Telefono = "88887777",
                Correo = "valentin@gmail.com",
                SitioWeb = "https://www.artesaniasmivo.com",
                Instagram = "https://www.instagram.com/artesaniasmivo",
                Facebook = "https://www.facebook.com/artesaniasmivo",
                ParticipaClubCreativo = true,
                ParticipaHechoEnCr = false,
                InformacionParticipacion =
                    "Deseo participar en ferias, talleres y actividades para promocionar mis productos artesanales."
            };

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarEmprendimiento(
            SolicitudEmprendimientoViewModel modelo)
        {
            ValidarArchivosSolicitud(modelo);

            if (!modelo.ParticipaClubCreativo &&
                !modelo.ParticipaHechoEnCr)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar al menos una opción de participación."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información suministrada es correcta."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajePerfil"] =
                "La información del emprendimiento fue actualizada correctamente.";

            return RedirectToAction(nameof(EditarEmprendimiento));
        }

        private void ValidarArchivosSolicitud(
    SolicitudEmprendimientoViewModel modelo)
        {
            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            // Validar logo si el usuario seleccionó uno.
            if (modelo.Logo is not null)
            {
                string extensionLogo =
                    Path.GetExtension(modelo.Logo.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extensionLogo))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Logo),
                        "El logo debe ser JPG, JPEG, PNG o WEBP."
                    );
                }

                if (modelo.Logo.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Logo),
                        "El logo puede pesar como máximo 5 MB."
                    );
                }
            }

            // Validar fotografías si fueron seleccionadas.
            if (modelo.Fotografias is not null &&
                modelo.Fotografias.Count > 0)
            {
                if (modelo.Fotografias.Count > 5)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fotografias),
                        "Puede seleccionar un máximo de cinco fotografías."
                    );
                }

                foreach (IFormFile fotografia in modelo.Fotografias)
                {
                    string extension =
                        Path.GetExtension(fotografia.FileName)
                            .ToLowerInvariant();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(modelo.Fotografias),
                            "Las fotografías deben ser JPG, JPEG, PNG o WEBP."
                        );

                        break;
                    }

                    if (fotografia.Length > tamanoMaximo)
                    {
                        ModelState.AddModelError(
                            nameof(modelo.Fotografias),
                            "Cada fotografía puede pesar como máximo 5 MB."
                        );

                        break;
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult PlanesSuscripcion()
        {
            return View(new SeleccionPlanViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlanesSuscripcion(
            SeleccionPlanViewModel modelo)
        {
            if (!modelo.ConfirmaSeleccion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaSeleccion),
                    "Debe confirmar la selección del plan."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeSuscripcion"] =
                $"El plan {modelo.PlanSeleccionado} fue seleccionado correctamente.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpGet]
        public IActionResult MiSuscripcion()
        {
            return View(new MiSuscripcionViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarSuscripcion(
            MiSuscripcionViewModel modelo)
        {
            if (!modelo.ConfirmaCancelacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaCancelacion),
                    "Debe confirmar que desea cancelar la suscripción."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "MiSuscripcion",
                    modelo
                );
            }

            TempData["MensajeSuscripcion"] =
                "La solicitud de cancelación fue enviada correctamente.";

            return RedirectToAction(nameof(MiSuscripcion));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RenovarSuscripcion()
        {
            TempData["MensajeSuscripcion"] =
                "La renovación automática fue activada correctamente.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        public IActionResult MisProductos()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CrearProducto()
        {
            return View(new ProductoEmprendedorViewModel
            {
                TipoPublicacion = "Producto",
                Estado = "Activo",
                Inventario = 1
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearProducto(
            ProductoEmprendedorViewModel modelo)
        {
            ValidarImagenesProducto(modelo);

            if (modelo.TipoPublicacion == "Producto" &&
                modelo.Inventario is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Inventario),
                    "Debe indicar la cantidad disponible."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información es correcta."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeProducto"] =
                "El producto o servicio fue creado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        private void ValidarImagenesProducto(
    ProductoEmprendedorViewModel modelo)
        {
            if (modelo.Imagenes is null ||
                modelo.Imagenes.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Debe seleccionar al menos una imagen."
                );

                return;
            }

            if (modelo.Imagenes.Count > 5)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Puede seleccionar un máximo de cinco imágenes."
                );
            }

            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            foreach (IFormFile imagen in modelo.Imagenes)
            {
                string extension =
                    Path.GetExtension(imagen.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Todas las imágenes deben ser JPG, JPEG, PNG o WEBP."
                    );

                    break;
                }

                if (imagen.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Cada imagen puede pesar como máximo 5 MB."
                    );

                    break;
                }
            }
        }

        [HttpGet]
        public IActionResult EditarProducto(int? id)
        {
            var modelo = new ProductoEmprendedorViewModel
            {
                Nombre = "Aretes artesanales",
                TipoPublicacion = "Producto",
                Categoria = "Accesorios y joyería",
                Descripcion =
                    "Aretes artesanales elaborados cuidadosamente a mano con materiales resistentes y diseños originales.",
                Etiquetas = "artesanal, accesorios, regalo",
                Precio = 8500,
                Inventario = 2,
                Estado = "Activo",
                PromocionAsociada = "Descuento 10%",
                EsDestacado = true
            };

            ViewBag.IdProducto = id ?? 1;

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProducto(
            int id,
            ProductoEmprendedorViewModel modelo)
        {
            ValidarImagenesOpcionalesProducto(modelo);

            if (modelo.TipoPublicacion == "Producto" &&
                modelo.Inventario is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Inventario),
                    "Debe indicar la cantidad disponible."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información es correcta."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdProducto = id;

                return View(modelo);
            }

            TempData["MensajeProducto"] =
                $"El producto «{modelo.Nombre}» fue actualizado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        private void ValidarImagenesOpcionalesProducto(
    ProductoEmprendedorViewModel modelo)
        {
            if (modelo.Imagenes is null ||
                modelo.Imagenes.Count == 0)
            {
                return;
            }

            if (modelo.Imagenes.Count > 5)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Puede seleccionar un máximo de cinco imágenes."
                );
            }

            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            foreach (IFormFile imagen in modelo.Imagenes)
            {
                string extension =
                    Path.GetExtension(imagen.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Todas las imágenes deben ser JPG, JPEG, PNG o WEBP."
                    );

                    break;
                }

                if (imagen.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Cada imagen puede pesar como máximo 5 MB."
                    );

                    break;
                }
            }
        }

        [HttpGet]
        public IActionResult Inventario()
        {
            return View(new AjustarInventarioViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjustarInventario(
            AjustarInventarioViewModel modelo)
        {
            if (!modelo.ConfirmaAjuste)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaAjuste),
                    "Debe confirmar el ajuste de inventario."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalInventario = true;

                return View(
                    "Inventario",
                    modelo
                );
            }

            TempData["MensajeInventario"] =
                $"El inventario de «{modelo.NombreProducto}» fue actualizado a " +
                $"{modelo.NuevaCantidad} unidades.";

            return RedirectToAction(nameof(Inventario));
        }

        [HttpGet]
        public IActionResult Ventas()
        {
            return View(new ActualizarVentaViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarEstadoVenta(
            ActualizarVentaViewModel modelo)
        {
            if (!modelo.ConfirmaCambio)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaCambio),
                    "Debe confirmar el cambio de estado de la venta."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalVenta = true;

                return View(
                    "Ventas",
                    modelo
                );
            }

            TempData["MensajeVenta"] =
                $"La orden {modelo.NumeroOrden} fue actualizada al estado " +
                $"«{modelo.NuevoEstado}» correctamente.";

            return RedirectToAction(nameof(Ventas));
        }

        [HttpGet]
        public IActionResult ParticipacionEventos()
        {
            return View(new SolicitudEventoViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SolicitarParticipacionEvento(
            SolicitudEventoViewModel modelo)
        {
            if (!modelo.ConfirmaSolicitud)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaSolicitud),
                    "Debe confirmar que desea enviar la solicitud."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalEvento = true;

                return View(
                    "ParticipacionEventos",
                    modelo
                );
            }

            TempData["MensajeEvento"] =
                $"La solicitud para participar en «{modelo.NombreEvento}» " +
                "fue enviada correctamente.";

            return RedirectToAction(nameof(ParticipacionEventos));
        }

        public IActionResult Estadisticas()
        {
            return View();
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "IniciarSesion",
                "Cuenta"
            );
        }
    }
}