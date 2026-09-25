using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Models.ViewModels;

namespace ProyectoClubCreativo.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ClubCreativoDbContext _context;

        public UsuarioController(ClubCreativoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Panel()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo"
                );

            if (usuario is null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            PanelUsuarioViewModel modelo = CrearPanelDemostrativo();

            modelo.NombreUsuario = usuario.Nombre;
            modelo.Correo = usuario.Correo;
            modelo.FotoPerfil = string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
                ? "/images/logo.jpg"
                : usuario.FotoPerfilUrl;

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
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

            string apellidos = usuario.ApellidoPaterno;

            if (!string.IsNullOrWhiteSpace(usuario.ApellidoMaterno))
            {
                apellidos += " " + usuario.ApellidoMaterno;
            }

            PerfilUsuarioViewModel modelo = new()
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellidos = apellidos,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono ?? string.Empty,
                IdProvincia = usuario.IdProvincia ?? 0,

                FechaNacimiento =
                    usuario.FechaNacimiento.HasValue
                        ? usuario.FechaNacimiento.Value
                            .ToDateTime(TimeOnly.MinValue)
                        : null,

                FotoActual =
                    string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
                        ? "/images/logo.jpg"
                        : usuario.FotoPerfilUrl,

                NotificacionesCompras = true,
                NotificacionesEventos = true,
                NotificacionesTalleres = true,
                NotificacionesPromociones = false,
                CanalCorreo = true,
                CanalPlataforma = true
            };

            await CargarProvinciasPerfilAsync(modelo);

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(
     PerfilUsuarioViewModel modelo)
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

            if (modelo.Fotografia is not null)
            {
                string[] extensionesPermitidas =
                [
                    ".jpg",
            ".jpeg",
            ".png",
            ".webp"
                ];

                string extension = Path
                    .GetExtension(modelo.Fotografia.FileName)
                    .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fotografia),
                        "Seleccione una imagen JPG, PNG o WEBP."
                    );
                }

                const long tamanoMaximo =
                    3 * 1024 * 1024;

                if (modelo.Fotografia.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fotografia),
                        "La fotografía no puede superar los 3 MB."
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(modelo.NuevaContrasena) &&
                string.IsNullOrWhiteSpace(modelo.ConfirmarContrasena))
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmarContrasena),
                    "Debe confirmar la nueva contraseña."
                );
            }

            string correoNormalizado =
                modelo.Correo?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(correoNormalizado))
            {
                bool correoEnUso = await _context.Usuarios
                    .AnyAsync(u =>
                        u.Correo.ToLower() == correoNormalizado &&
                        u.IdUsuario != usuario.IdUsuario
                    );

                if (correoEnUso)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Correo),
                        "Ya existe otra cuenta registrada con este correo electrónico."
                    );
                }
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
            }

            if (!ModelState.IsValid)
            {
                modelo.FotoActual =
                    string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
                        ? "/images/logo.jpg"
                        : usuario.FotoPerfilUrl;

                await CargarProvinciasPerfilAsync(modelo);

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

            usuario.Nombre = modelo.Nombre.Trim();
            usuario.ApellidoPaterno = apellidoPaterno;
            usuario.ApellidoMaterno = apellidoMaterno;
            usuario.Correo = correoNormalizado;
            usuario.Telefono = modelo.Telefono.Trim();
            usuario.IdProvincia = modelo.IdProvincia;

            usuario.FechaNacimiento =
                modelo.FechaNacimiento.HasValue
                    ? DateOnly.FromDateTime(
                        modelo.FechaNacimiento.Value
                    )
                    : null;

            if (!string.IsNullOrWhiteSpace(modelo.NuevaContrasena))
            {
                usuario.ContrasenaHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        modelo.NuevaContrasena
                    );
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString(
                "NombreUsuario",
                usuario.Nombre
            );

            HttpContext.Session.SetString(
                "CorreoUsuario",
                usuario.Correo
            );

            TempData["MensajePerfil"] =
                "Tus datos personales se actualizaron correctamente.";

            return RedirectToAction(nameof(Perfil));
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("IniciarSesion", "Cuenta");
        }

        private static PanelUsuarioViewModel CrearPanelDemostrativo()
        {
            return new PanelUsuarioViewModel
            {
                NombreUsuario = "Maria",
                Correo = "maria@ejemplo.com",
                FotoPerfil = "/images/logo.jpg",
                PuntosAcumulados = 1280,
                CantidadFavoritos = 12,
                NotificacionesPendientes = 4,

                ProximosEventos =
                [
                    new EventoUsuarioViewModel
                    {
                        Nombre = "Arte Inarrivo San Pedro",
                        Fecha = "15 y 16 de agosto",
                        Ubicacion = "San Pedro",
                        Imagen = "/images/evento-san-pedro.jpg"
                    },
                    new EventoUsuarioViewModel
                    {
                        Nombre = "Feria Creativa Santa Ana",
                        Fecha = "5 al 7 de septiembre",
                        Ubicacion = "Santa Ana",
                        Imagen = "/images/evento-santa-ana.jpg"
                    }
                ],

                TalleresReservados =
                [
                    new TallerUsuarioViewModel
                    {
                        Nombre = "Cerámica para principiantes",
                        Fecha = "23 de agosto",
                        Hora = "10:00 a. m.",
                        Estado = "Confirmado"
                    },
                    new TallerUsuarioViewModel
                    {
                        Nombre = "Bordado creativo",
                        Fecha = "30 de agosto",
                        Hora = "2:00 p. m.",
                        Estado = "Pendiente"
                    }
                ],

                PedidosRecientes =
                [
                    new PedidoUsuarioViewModel
                    {
                        NumeroOrden = "CC-1025",
                        Fecha = "2 de agosto de 2026",
                        Total = 18500,
                        Estado = "En preparación"
                    },
                    new PedidoUsuarioViewModel
                    {
                        NumeroOrden = "CC-1008",
                        Fecha = "25 de julio de 2026",
                        Total = 32000,
                        Estado = "Entregado"
                    }
                ],

                Notificaciones =
                [
                    new NotificacionUsuarioViewModel
                    {
                        Tipo = "Evento",
                        Mensaje = "Tu inscripción al evento de San Pedro fue confirmada.",
                        Fecha = "Hoy",
                        Icono = "bi-calendar-event"
                    },
                    new NotificacionUsuarioViewModel
                    {
                        Tipo = "Compra",
                        Mensaje = "El pedido CC-1025 se encuentra en preparación.",
                        Fecha = "Ayer",
                        Icono = "bi-bag-check"
                    },
                    new NotificacionUsuarioViewModel
                    {
                        Tipo = "Puntos",
                        Mensaje = "Ganaste 180 puntos por tu última compra.",
                        Fecha = "2 de agosto",
                        Icono = "bi-star-fill"
                    }
                ]
            };
        }

        [HttpGet]
        public IActionResult MisCompras()
        {
            List<CompraUsuarioViewModel> modelo =
            [
                new()
        {
            NumeroOrden = "CC-1025",
            Fecha = new DateTime(2026, 8, 2),
            Total = 18500,
            Estado = "En preparación",
            MetodoEntrega = "Retiro en feria"
        },
        new()
        {
            NumeroOrden = "CC-1008",
            Fecha = new DateTime(2026, 7, 25),
            Total = 32000,
            Estado = "Entregado",
            MetodoEntrega = "Envío a domicilio"
        },
        new()
        {
            NumeroOrden = "CC-0984",
            Fecha = new DateTime(2026, 7, 10),
            Total = 12750,
            Estado = "Entregado",
            MetodoEntrega = "Retiro en feria"
        },
        new()
        {
            NumeroOrden = "CC-0950",
            Fecha = new DateTime(2026, 6, 18),
            Total = 24400,
            Estado = "Cancelado",
            MetodoEntrega = "Envío a domicilio"
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult DetalleCompra(string id = "CC-1025")
        {
            DetalleCompraUsuarioViewModel modelo = new()
            {
                NumeroOrden = id,
                Fecha = new DateTime(2026, 8, 2),
                Estado = "En preparación",
                MetodoEntrega = "Retiro en Feria Creativa San Pedro",
                DireccionEntrega = "Punto de retiro del evento",
                Subtotal = 20500,
                Descuento = 2000,
                Total = 18500,

                Productos =
                [
                    new()
            {
                IdProducto = 1,
                Nombre = "Aretes Orquídea",
                Emprendimiento = "Orquídea",
                Imagen = "/images/producto-1.jpg",
                Cantidad = 1,
                Precio = 12000
            },
            new()
            {
                IdProducto = 2,
                Nombre = "Vela Antojo de Churchill",
                Emprendimiento = "Luz Natural",
                Imagen = "/images/producto-3.jpg",
                Cantidad = 1,
                Precio = 8500
            }
                ]
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Carrito()
        {
            CarritoUsuarioViewModel modelo = new()
            {
                Descuento = 2000,

                Productos =
                [
                    new()
            {
                IdProducto = 1,
                Nombre = "Aretes Orquídea",
                Emprendimiento = "Orquídea",
                Imagen = "/images/producto-1.jpg",
                Cantidad = 1,
                Precio = 12000
            },
            new()
            {
                IdProducto = 2,
                Nombre = "Vela Antojo de Churchill",
                Emprendimiento = "Luz Natural",
                Imagen = "/images/producto-3.jpg",
                Cantidad = 1,
                Precio = 8500
            }
                ]
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ProcesoCompra()
        {
            ProcesoCompraViewModel modelo = new()
            {
                Nombre = "Maria",
                Correo = "maria@ejemplo.com",
                Telefono = "88888888",
                Subtotal = 20500,
                Descuento = 2000,
                Total = 18500
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcesoCompra(ProcesoCompraViewModel modelo)
        {
            if (modelo.TipoEntrega == "Domicilio" &&
                string.IsNullOrWhiteSpace(modelo.Direccion))
            {
                ModelState.AddModelError(
                    nameof(modelo.Direccion),
                    "Ingrese la dirección de entrega."
                );
            }

            modelo.Subtotal = 20500;
            modelo.Descuento = 2000;
            modelo.Total = 18500;

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeCompra"] =
                "La información de la compra se validó correctamente.";

            return RedirectToAction(nameof(DetalleCompra), new { id = "CC-1026" });
        }

        [HttpGet]
        public IActionResult MisInscripciones()
        {
            List<InscripcionUsuarioViewModel> modelo =
            [
                new()
        {
            Id = 1,
            Tipo = "Evento",
            Nombre = "Feria Creativa San Pedro",
            Fecha = "15 y 16 de agosto",
            Ubicacion = "San Pedro",
            Estado = "Próximo",
            Imagen = "/images/evento-san-pedro.jpg"
        },
        new()
        {
            Id = 2,
            Tipo = "Taller",
            Nombre = "Cerámica para principiantes",
            Fecha = "23 de agosto, 10:00 a. m.",
            Ubicacion = "Escazú",
            Estado = "Próximo",
            Imagen = "/images/taller-ceramica.jpg"
        },
        new()
        {
            Id = 3,
            Tipo = "Evento",
            Nombre = "Feria Creativa Heredia",
            Fecha = "20 de julio",
            Ubicacion = "Heredia",
            Estado = "Finalizado",
            Imagen = "/images/evento-madres.jpg"
        },
        new()
        {
            Id = 4,
            Tipo = "Taller",
            Nombre = "Ilustración botánica",
            Fecha = "10 de julio",
            Ubicacion = "San José",
            Estado = "Cancelado",
            Imagen = "/images/taller-bordado.jpg"
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult PuntosRecompensas()
        {
            PuntosRecompensasViewModel modelo = new()
            {
                NombreUsuario = "Maria",
                SaldoPuntos = 1280,
                CodigoCliente = "CC-MARIA-1280",

                Movimientos =
                [
                    new()
            {
                Fecha = new DateTime(2026, 8, 2),
                Descripcion = "Compra CC-1025",
                Puntos = 180,
                Tipo = "Ganados"
            },
            new()
            {
                Fecha = new DateTime(2026, 7, 25),
                Descripcion = "Compra CC-1008",
                Puntos = 320,
                Tipo = "Ganados"
            },
            new()
            {
                Fecha = new DateTime(2026, 7, 18),
                Descripcion = "Canje de descuento",
                Puntos = -250,
                Tipo = "Canjeados"
            }
                ],

                Recompensas =
                [
                    new()
            {
                Id = 1,
                Nombre = "10 % de descuento",
                Descripcion = "Aplicable en una compra seleccionada.",
                CostoPuntos = 500,
                Icono = "bi-percent",
                Disponible = true
            },
            new()
            {
                Id = 2,
                Nombre = "Entrada especial",
                Descripcion = "Acceso prioritario a una feria.",
                CostoPuntos = 900,
                Icono = "bi-ticket-perforated-fill",
                Disponible = true
            },
            new()
            {
                Id = 3,
                Nombre = "Taller gratuito",
                Descripcion = "Canje por un taller participante.",
                CostoPuntos = 1500,
                Icono = "bi-palette-fill",
                Disponible = false
            }
                ]
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Favoritos()
        {
            List<FavoritoUsuarioViewModel> modelo =
            [
                new()
        {
            Id = 1,
            Tipo = "Producto",
            Nombre = "Aretes Orquídea",
            Descripcion = "Accesorios artesanales",
            Imagen = "/images/producto-1.jpg"
        },
        new()
        {
            Id = 2,
            Tipo = "Emprendimiento",
            Nombre = "Luz Natural",
            Descripcion = "Velas artesanales",
            Imagen = "/images/producto-3.jpg"
        },
        new()
        {
            Id = 3,
            Tipo = "Evento",
            Nombre = "Feria Creativa Santa Ana",
            Descripcion = "5 al 7 de septiembre",
            Imagen = "/images/evento-santa-ana.jpg"
        },
        new()
        {
            Id = 4,
            Tipo = "Taller",
            Nombre = "Bordado creativo",
            Descripcion = "Taller para principiantes",
            Imagen = "/images/taller-bordado.jpg"
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Comentarios()
        {
            List<ComentarioUsuarioViewModel> modelo =
            [
                new()
        {
            Id = 1,
            Elemento = "Feria Creativa San Pedro",
            TipoElemento = "Evento",
            Comentario = "La organización estuvo muy bien y había gran variedad de emprendimientos.",
            Valoracion = 5,
            Fecha = new DateTime(2026, 7, 20)
        },
        new()
        {
            Id = 2,
            Elemento = "Aretes Orquídea",
            TipoElemento = "Producto",
            Comentario = "El producto es bonito y llegó en excelentes condiciones.",
            Valoracion = 4,
            Fecha = new DateTime(2026, 7, 12)
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Encuestas()
        {
            List<EncuestaUsuarioViewModel> modelo =
            [
                new()
        {
            Id = 1,
            Nombre = "Experiencia en Feria Creativa",
            ElementoRelacionado = "Feria Creativa San Pedro",
            TipoElemento = "Evento",
            Estado = "Pendiente",
            FechaLimite = "30 de agosto"
        },
        new()
        {
            Id = 2,
            Nombre = "Evaluación del taller",
            ElementoRelacionado = "Cerámica para principiantes",
            TipoElemento = "Taller",
            Estado = "Pendiente",
            FechaLimite = "5 de septiembre"
        },
        new()
        {
            Id = 3,
            Nombre = "Satisfacción general",
            ElementoRelacionado = "Feria Creativa Heredia",
            TipoElemento = "Evento",
            Estado = "Respondida",
            FechaLimite = "Respondida el 22 de julio"
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ResponderEncuesta(int id = 1)
        {
            ResponderEncuestaViewModel modelo = new()
            {
                IdEncuesta = id,
                NombreEncuesta = "Experiencia en Feria Creativa",
                ElementoRelacionado = "Feria Creativa San Pedro"
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResponderEncuesta(ResponderEncuestaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeEncuesta"] =
                "La encuesta fue validada y enviada correctamente.";

            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public IActionResult Notificaciones()
        {
            List<NotificacionListadoViewModel> modelo =
            [
                new()
        {
            Id = 1,
            Tipo = "Evento",
            Titulo = "Inscripción confirmada",
            Mensaje = "Tu inscripción a la Feria Creativa San Pedro fue confirmada.",
            Fecha = "Hoy, 9:15 a. m.",
            Icono = "bi-calendar-check-fill",
            Leida = false
        },
        new()
        {
            Id = 2,
            Tipo = "Compra",
            Titulo = "Pedido en preparación",
            Mensaje = "El pedido CC-1025 se encuentra en preparación.",
            Fecha = "Ayer, 4:30 p. m.",
            Icono = "bi-bag-check-fill",
            Leida = false
        },
        new()
        {
            Id = 3,
            Tipo = "Puntos",
            Titulo = "Ganaste nuevos puntos",
            Mensaje = "Se acreditaron 180 puntos a tu tarjeta Creativo Frecuente.",
            Fecha = "2 de agosto",
            Icono = "bi-star-fill",
            Leida = false
        },
        new()
        {
            Id = 4,
            Tipo = "Promoción",
            Titulo = "Nueva promoción disponible",
            Mensaje = "Canjea tus puntos por un descuento especial.",
            Fecha = "1 de agosto",
            Icono = "bi-megaphone-fill",
            Leida = true
        }
            ];

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Preferencias()
        {
            PreferenciasNotificacionViewModel modelo = new()
            {
                Compras = true,
                Eventos = true,
                Talleres = true,
                Promociones = false,
                Recordatorios = true,
                CanalCorreo = true,
                CanalPlataforma = true,
                FrecuenciaCorreo = "Inmediata"
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Preferencias(
            PreferenciasNotificacionViewModel modelo
        )
        {
            if (!modelo.CanalCorreo && !modelo.CanalPlataforma)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Seleccione al menos un canal de notificación."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajePreferencias"] =
                "Las preferencias se validaron correctamente.";

            return RedirectToAction(nameof(Preferencias));
        }

        [HttpGet]
        public IActionResult MiQr()
        {
            return View();
        }

        private async Task CargarProvinciasPerfilAsync(
    PerfilUsuarioViewModel modelo)
        {
            modelo.Provincias = await _context.Provincias
                .OrderBy(p => p.Nombre)
                .Select(p =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = p.IdProvincia.ToString(),
                        Text = p.Nombre
                    })
                .ToListAsync();
        }

        // ---------- SOLICITUD PARA FORMAR PARTE DE CLUB CREATIVO ----------
        [HttpGet]
        public async Task<IActionResult> SolicitarEmprendimiento(int? idCategoria)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (usuario is null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool yaTieneEmprendimiento =
                await _context.Emprendimientos.AnyAsync(e =>
                    e.IdUsuarioPropietario == usuario.IdUsuario);

            if (yaTieneEmprendimiento)
            {
                TempData["MensajePanel"] =
                    "Ya existe una solicitud de emprendimiento asociada a tu cuenta.";

                return RedirectToAction(nameof(Panel));
            }

            SolicitudEmprendimientoViewModel modelo = new()
            {
                NombreComercial = string.Empty,
                Descripcion = string.Empty,
                IdCategoria = idCategoria,
                Cedula = string.Empty,
                Telefono = usuario.Telefono ?? string.Empty,
                Correo = usuario.Correo,
                ParticipaClubCreativo = true,
                ParticipaHechoEnCr = false,
                InformacionParticipacion = string.Empty,
                ConfirmaInformacion = false
            };

            modelo.Categorias = await CargarCategoriasEmprendimientoAsync();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarEmprendimiento(
            SolicitudEmprendimientoViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información suministrada es correcta."
                );
            }

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
                modelo.Categorias = await CargarCategoriasEmprendimientoAsync();
                return View(modelo);
            }

            Usuario? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (usuario is null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool yaTieneEmprendimiento =
                await _context.Emprendimientos.AnyAsync(e =>
                    e.IdUsuarioPropietario == usuario.IdUsuario);

            if (yaTieneEmprendimiento)
            {
                TempData["MensajePanel"] =
                    "Ya existe una solicitud de emprendimiento asociada a tu cuenta.";

                return RedirectToAction(nameof(Panel));
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Emprendimiento emprendimiento = new()
                {
                    IdUsuarioPropietario = usuario.IdUsuario,
                    IdCategoria = modelo.IdCategoria,
                    NombreComercial = modelo.NombreComercial.Trim(),
                    Descripcion = modelo.Descripcion.Trim(),
                    CedulaJuridica = modelo.Cedula.Trim(),
                    Telefono = modelo.Telefono.Trim(),
                    Correo = modelo.Correo.Trim().ToLowerInvariant(),
                    SitioWeb = string.IsNullOrWhiteSpace(modelo.SitioWeb)
                        ? null
                        : modelo.SitioWeb.Trim(),
                    Instagram = string.IsNullOrWhiteSpace(modelo.Instagram)
                        ? null
                        : modelo.Instagram.Trim(),
                    Facebook = string.IsNullOrWhiteSpace(modelo.Facebook)
                        ? null
                        : modelo.Facebook.Trim(),
                    LogoUrl = null,
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

                TempData["MensajePanel"] =
                    "Tu solicitud para formar parte de Club Creativo fue enviada correctamente y se encuentra pendiente de revisión.";

                return RedirectToAction(nameof(Panel));
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al enviar la solicitud. Intente nuevamente."
                );

                modelo.Categorias = await CargarCategoriasEmprendimientoAsync();

                return View(modelo);
            }
        }

        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>>
            CargarCategoriasEmprendimientoAsync()
        {
            return await _context.Categorias
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
        }

    }
}
