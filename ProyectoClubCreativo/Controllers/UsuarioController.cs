using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Models.ViewModels;
using ProyectoClubCreativo.Services;
using System.Text.Json;

namespace ProyectoClubCreativo.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ClubCreativoDbContext _context;
        private readonly PagoService _pagoService;

        public UsuarioController(
    ClubCreativoDbContext context,
    PagoService pagoService)
        {
            _context = context;
            _pagoService = pagoService;
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

            DateTime ahora = DateTime.Now;
            int id = idUsuario.Value;

            var eventos = await _context.InscripcionesEventos
                .AsNoTracking()
                .Where(i =>
                    i.IdUsuario == id &&
                    i.Estado != "Cancelada" &&
                    i.IdEventoNavigation.FechaInicio >= ahora)
                .OrderBy(i => i.IdEventoNavigation.FechaInicio)
                .Take(3)
                .Select(i => new
                {
                    i.IdEventoNavigation.Nombre,
                    i.IdEventoNavigation.FechaInicio,
                    i.IdEventoNavigation.Ubicacion,
                    i.IdEventoNavigation.ImagenUrl
                })
                .ToListAsync();

            var talleres = await _context.InscripcionesTallers
                .AsNoTracking()
                .Where(i =>
                    i.IdUsuario == id &&
                    i.Estado != "Cancelada" &&
                    i.IdTallerNavigation.Estado == ReglasTalleres.Programado &&
                    i.IdTallerNavigation.FechaInicio != null &&
                    i.IdTallerNavigation.FechaInicio >= ahora)
                .OrderBy(i => i.IdTallerNavigation.FechaInicio)
                .Take(3)
                .Select(i => new
                {
                    i.IdTallerNavigation.Nombre,
                    FechaInicio = i.IdTallerNavigation.FechaInicio!.Value,
                    i.Estado
                })
                .ToListAsync();

            var pedidos = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.IdUsuario == id)
                .OrderByDescending(v => v.FechaVenta)
                .Take(3)
                .Select(v => new { v.NumeroOrden, v.FechaVenta, v.Total, v.Estado })
                .ToListAsync();

            var notificaciones = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.IdUsuario == id)
                .OrderByDescending(n => n.FechaEnvio)
                .Take(3)
                .Select(n => new { n.Tipo, n.Mensaje, n.FechaEnvio })
                .ToListAsync();

            PanelUsuarioViewModel modelo = new()
            {
                NombreUsuario = usuario.Nombre,
                Correo = usuario.Correo,
                FotoPerfil = string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
                    ? "/images/logo.jpg"
                    : usuario.FotoPerfilUrl,
                PuntosAcumulados = await _context.MovimientosPuntos
                    .Where(m => m.IdUsuario == id)
                    .SumAsync(m => (int?)m.Puntos) ?? 0,
                CantidadFavoritos = await _context.Favoritos
                    .CountAsync(f => f.IdUsuario == id),
                NotificacionesPendientes = await _context.Notificaciones
                    .CountAsync(n => n.IdUsuario == id && !n.Leida),

                ProximosEventos = eventos
                    .Select(e => new EventoUsuarioViewModel
                    {
                        Nombre = e.Nombre,
                        Fecha = e.FechaInicio.ToString("d 'de' MMMM", CulturaCr),
                        Ubicacion = e.Ubicacion,
                        Imagen = string.IsNullOrWhiteSpace(e.ImagenUrl)
                            ? "/images/hero-feria.jpg"
                            : e.ImagenUrl
                    })
                    .ToList(),

                TalleresReservados = talleres
                    .Select(x => new TallerUsuarioViewModel
                    {
                        Nombre = x.Nombre,
                        Fecha = x.FechaInicio.ToString("d 'de' MMMM", CulturaCr),
                        Hora = x.FechaInicio.ToString("h:mm tt", CulturaCr),
                        Estado = x.Estado
                    })
                    .ToList(),

                PedidosRecientes = pedidos
                    .Select(v => new PedidoUsuarioViewModel
                    {
                        NumeroOrden = v.NumeroOrden,
                        Fecha = v.FechaVenta.ToString("d 'de' MMMM 'de' yyyy", CulturaCr),
                        Total = v.Total,
                        Estado = v.Estado
                    })
                    .ToList(),

                Notificaciones = notificaciones
                    .Select(n => new NotificacionUsuarioViewModel
                    {
                        Tipo = n.Tipo,
                        Mensaje = n.Mensaje,
                        Fecha = n.FechaEnvio.ToString("dd/MM/yyyy"),
                        Icono = n.Tipo == "Compra"
                            ? "bi-bag-check"
                            : n.Tipo == "Evento"
                                ? "bi-calendar-event"
                                : n.Tipo == "Puntos"
                                    ? "bi-star-fill"
                                    : "bi-bell-fill"
                    })
                    .ToList()
            };

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

        [HttpGet]
        public async Task<IActionResult> MisCompras(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            int? idEmprendimiento)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var consulta = _context.Ventas
                .AsNoTracking()
                .Where(v => v.IdUsuario == idUsuario.Value);

            if (fechaInicio.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.FechaVenta >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                DateTime fechaLimite = fechaFin.Value.Date.AddDays(1);

                consulta = consulta.Where(v =>
                    v.FechaVenta < fechaLimite);
            }

            if (idEmprendimiento.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.VentaDetalles.Any(d =>
                        d.IdProductoNavigation.IdEmprendimiento
                            == idEmprendimiento.Value));
            }

            var ventas = await consulta
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            var modelo = ventas.Select(v => new CompraUsuarioViewModel
            {
                NumeroOrden = v.NumeroOrden,
                Fecha = v.FechaVenta,
                Total = v.Total,
                Estado = v.Estado,
                MetodoEntrega = v.TipoEntrega == "Domicilio"
                    ? "Envío a domicilio"
                    : "Retiro"
            }).ToList();

            var emprendimientos = await _context.VentaDetalles
                .AsNoTracking()
                .Where(d =>
                    d.IdVentaNavigation.IdUsuario == idUsuario.Value)
                .Select(d => new
                {
                    Id = d.IdProductoNavigation.IdEmprendimiento,
                    Nombre = d.IdProductoNavigation
                        .IdEmprendimientoNavigation.NombreComercial
                })
                .Distinct()
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Emprendimientos = new SelectList(
                emprendimientos,
                "Id",
                "Nombre",
                idEmprendimiento);

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.IdEmprendimiento = idEmprendimiento;

            return View(modelo);
        }





        [HttpGet]
        public async Task<IActionResult> DetalleCompra(string id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.VentaDetalles)
                    .ThenInclude(d => d.IdProductoNavigation)
                        .ThenInclude(p => p.ProductoImagene)
                .Include(v => v.VentaDetalles)
                    .ThenInclude(d => d.IdProductoNavigation)
                        .ThenInclude(p => p.IdEmprendimientoNavigation)
                .FirstOrDefaultAsync(v =>
                    v.NumeroOrden == id &&
                    v.IdUsuario == idUsuario.Value);

            if (venta == null)
            {
                return NotFound();
            }

            int puntosGanados = await _context.MovimientosPuntos
     .AsNoTracking()
     .Where(m =>
         m.IdVenta == venta.IdVenta &&
         m.IdUsuario == idUsuario.Value &&
         (m.TipoMovimiento == "Acumulacion" ||
          m.TipoMovimiento == "Reversion"))
     .SumAsync(m => (int?)m.Puntos) ?? 0;

            DetalleCompraUsuarioViewModel modelo = new()
            {
                NumeroOrden = venta.NumeroOrden,
                Fecha = venta.FechaVenta,
                Estado = venta.Estado,

                MetodoEntrega = venta.TipoEntrega == "Domicilio"
                    ? "Entrega a domicilio"
                    : "Retiro",

                DireccionEntrega = venta.TipoEntrega == "Domicilio"
                    ? venta.DireccionEntrega ?? ""
                    : "Punto de retiro por coordinar",

                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Total = venta.Total,
                PuntosGanados = puntosGanados,

                Productos = venta.VentaDetalles.Select(d =>
     new ProductoCompraViewModel
     {
                        IdProducto = d.IdProducto,
                        Nombre = d.IdProductoNavigation.Nombre,
                        Emprendimiento =
                            d.IdProductoNavigation
                                .IdEmprendimientoNavigation
                                .NombreComercial,

        Imagen = d.IdProductoNavigation
    .ProductoImagene?.UrlImagen ?? "/images/logo.jpg",

        Cantidad = d.Cantidad,
                        Precio = d.PrecioUnitario
                    }).ToList()
            };

            List<int> idsProductosCompra = modelo.Productos
    .Select(p => p.IdProducto)
    .ToList();

            List<ComentariosResena> resenasPrevias =
                await _context.ComentariosResenas
                    .AsNoTracking()
                    .Where(c =>
                        c.IdUsuario == idUsuario.Value &&
                        c.IdProducto != null &&
                        idsProductosCompra.Contains(c.IdProducto.Value) &&
                        c.Estado != "Eliminado")
                    .ToListAsync();

            foreach (ProductoCompraViewModel producto in modelo.Productos)
            {
                ComentariosResena? resenaPrevia =
                    resenasPrevias.FirstOrDefault(r =>
                        r.IdProducto == producto.IdProducto);

                producto.ValoracionPrevia = resenaPrevia?.Valoracion;
                producto.ComentarioPrevio = resenaPrevia?.Contenido;
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalificarProducto(
    ResenaProductoViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (!ModelState.IsValid)
            {
                return RegresarConErrorResena(
                    modelo,
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .First());
            }

            string[] estadosCalificables =
 {
                "Pagada", "En preparacion", "Enviada", "Entregada"
            };

            bool compraCalificable = await _context.VentaDetalles
                .AnyAsync(d =>
                    d.IdProducto == modelo.IdProducto &&
                    d.IdVentaNavigation.NumeroOrden == modelo.NumeroOrden &&
                    d.IdVentaNavigation.IdUsuario == idUsuario.Value &&
                    estadosCalificables.Contains(d.IdVentaNavigation.Estado));

            if (!compraCalificable)
            {
                return RegresarConErrorResena(
                    modelo,
                    "Solo puedes calificar productos de compras con pago confirmado.");
            }

            string contenido = modelo.Contenido?.Trim() ?? string.Empty;

            ComentariosResena? resena =
                await _context.ComentariosResenas
                    .FirstOrDefaultAsync(c =>
                        c.IdUsuario == idUsuario.Value &&
                        c.IdProducto == modelo.IdProducto &&
                        c.Estado != "Eliminado");

            bool esEdicion = resena is not null;

            if (resena is null)
            {
                resena = new ComentariosResena
                {
                    IdUsuario = idUsuario.Value,
                    IdProducto = modelo.IdProducto,
                    Contenido = contenido,
                    Valoracion = modelo.Valoracion,
                    Estado = "Visible",
                    CantidadReportes = 0,
                    FechaPublicacion = DateTime.Now
                };

                _context.ComentariosResenas.Add(resena);
            }
            else
            {
                resena.Valoracion = modelo.Valoracion;
                resena.Contenido = contenido;
                resena.FechaEdicion = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["MensajeCompra"] = esEdicion
                ? "Tu reseña fue actualizada correctamente."
                : "¡Gracias! Tu calificación se registró correctamente.";

            return RedirectToAction(
                nameof(DetalleCompra),
                new { id = modelo.NumeroOrden });
        }


        private IActionResult RegresarConErrorResena(
            ResenaProductoViewModel modelo,
            string mensaje)
        {
            TempData["ErrorResena"] = mensaje;
            TempData["ResenaIdProducto"] = modelo.IdProducto;
            TempData["ResenaContenido"] = modelo.Contenido ?? string.Empty;

            return RedirectToAction(
                nameof(DetalleCompra),
                new { id = modelo.NumeroOrden });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAlCarrito(int idProducto)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var producto = await _context.Productos
    .AsNoTracking()
    .Include(p => p.ProductoImagene)
    .Include(p => p.IdEmprendimientoNavigation)
    .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

            if (producto == null ||
                producto.TipoPublicacion != "Producto" ||
                producto.Estado != "Publicado" ||
                producto.StockActual <= 0)
            {
                TempData["ErrorCarrito"] =
                    "Este producto no está disponible para la compra.";

                return RedirectToAction(
                    "DetalleProducto",
                    "Home",
                    new { id = idProducto });
            }

            var carrito = ObtenerCarrito();

            var productoEnCarrito = carrito
                .FirstOrDefault(p => p.IdProducto == producto.IdProducto);

            int cantidadActual = productoEnCarrito?.Cantidad ?? 0;

            if (cantidadActual >= producto.StockActual)
            {
                TempData["ErrorCarrito"] =
                    "No puedes agregar más unidades de las disponibles.";

                return RedirectToAction(
                    "DetalleProducto",
                    "Home",
                    new { id = idProducto });
            }

            if (productoEnCarrito != null)
            {
                productoEnCarrito.Cantidad++;
            }
            else
            {

                carrito.Add(new ProductoCompraViewModel
                {
                    IdProducto = producto.IdProducto,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = 1,
                    Imagen = producto.ProductoImagene?.UrlImagen
                        ?? "/images/logo.jpg",
                    Emprendimiento =
                        producto.IdEmprendimientoNavigation?.NombreComercial
                        ?? "Emprendimiento"
                });

            }

            GuardarCarrito(carrito);

            TempData["MensajeCarrito"] =
                "Producto agregado al carrito correctamente.";

            return RedirectToAction(nameof(Carrito));

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarDelCarrito(int idProducto)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var carrito = ObtenerCarrito();

            carrito.RemoveAll(p => p.IdProducto == idProducto);

            GuardarCarrito(carrito);

            TempData["MensajeCarrito"] =
                "Producto eliminado del carrito correctamente.";

            return RedirectToAction(nameof(Carrito));
        }


        private List<ProductoCompraViewModel> ObtenerCarrito()
        {
            string? carritoJson =
                HttpContext.Session.GetString("CarritoProductos");

            if (string.IsNullOrEmpty(carritoJson))
            {
                return new List<ProductoCompraViewModel>();
            }

            return JsonSerializer.Deserialize<List<ProductoCompraViewModel>>(
                carritoJson
            ) ?? new List<ProductoCompraViewModel>();
        }

        private void GuardarCarrito(List<ProductoCompraViewModel> productos)
        {
            string carritoJson = JsonSerializer.Serialize(productos);

            HttpContext.Session.SetString(
                "CarritoProductos",
                carritoJson
            );
        }


        private async Task<bool> DescontarInventarioAsync(
            int idProducto,
            int cantidad)
        {
            if (cantidad <= 0)
            {
                return false;
            }

            int filasActualizadas = await _context.Productos
                .Where(p =>
                    p.IdProducto == idProducto &&
                    p.TipoPublicacion == "Producto" &&
                    p.Estado == "Publicado" &&
                    p.StockActual >= cantidad)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        p => p.StockActual,
                        p => p.StockActual - cantidad
                    )
                );

            return filasActualizadas == 1;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarCantidadCarrito(
            int idProducto, int cambio)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var carrito = ObtenerCarrito();

            var productoCarrito = carrito
                .FirstOrDefault(p => p.IdProducto == idProducto);

            if (productoCarrito == null)
            {
                return RedirectToAction(nameof(Carrito));
            }

            if (cambio != 1 && cambio != -1)
            {
                return BadRequest();
            }

            int nuevaCantidad = productoCarrito.Cantidad + cambio;

            if (nuevaCantidad < 1)
            {
                TempData["ErrorCarrito"] =
                    "La cantidad mínima es una unidad.";

                return RedirectToAction(nameof(Carrito));
            }

            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

            if (producto == null ||
                producto.TipoPublicacion != "Producto" ||
                producto.Estado != "Publicado" ||
                producto.StockActual <= 0)
            {
                TempData["ErrorCarrito"] =
                    "Este producto ya no está disponible.";

                return RedirectToAction(nameof(Carrito));
            }

            if (nuevaCantidad > producto.StockActual)
            {
                TempData["ErrorCarrito"] =
                    "No hay suficientes unidades disponibles.";

                return RedirectToAction(nameof(Carrito));
            }

            productoCarrito.Cantidad = nuevaCantidad;

            GuardarCarrito(carrito);

            return RedirectToAction(nameof(Carrito));
        }


        [HttpGet]
        public IActionResult Carrito()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var productos = ObtenerCarrito();

            CarritoUsuarioViewModel modelo = new()
            {
                Productos = productos,
                Descuento = 0
            };

            return View(modelo);
        }



        [HttpGet]
        public async Task<IActionResult> ProcesoCompra()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] =
                    "Debes agregar al menos un producto antes de continuar.";

                return RedirectToAction(nameof(Carrito));
            }

            foreach (var item in carrito)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto);

                if (producto == null ||
                    producto.TipoPublicacion != "Producto" ||
                    producto.Estado != "Publicado" ||
                    producto.StockActual < item.Cantidad ||
                    item.Cantidad <= 0)
                {
                    TempData["ErrorCarrito"] =
                        "Uno o más productos no tienen existencias suficientes. Revisa tu carrito.";

                    return RedirectToAction(nameof(Carrito));
                }
            }

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstAsync(u => u.IdUsuario == idUsuario.Value);

            ProcesoCompraViewModel modelo = new()
            {
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono ?? string.Empty,
                Subtotal = carrito.Sum(p => p.Subtotal),
                Descuento = 0,
                Total = carrito.Sum(p => p.Subtotal)
            };

            return View(modelo);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesoCompra(
            ProcesoCompraViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            if (modelo.TipoEntrega == "Domicilio" &&
                string.IsNullOrWhiteSpace(modelo.Direccion))
            {
                ModelState.AddModelError(
                    nameof(modelo.Direccion),
                    "Ingrese la dirección de entrega.");
            }

            if (!ModelState.IsValid)
            {
                modelo.Subtotal = carrito.Sum(p => p.Subtotal);
                modelo.Descuento = 0;
                modelo.Total = modelo.Subtotal;

                return View(modelo);
            }

            if (modelo.TipoEntrega != "Retiro" &&
                modelo.TipoEntrega != "Domicilio")
            {
                ModelState.AddModelError(
                    nameof(modelo.TipoEntrega),
                    "Seleccione un tipo de entrega válido.");
            }

            if (modelo.MetodoPago != "Tarjeta de crédito o débito" &&
                modelo.MetodoPago != "SINPE Móvil" &&
                modelo.MetodoPago != "Pago al retirar")
            {
                ModelState.AddModelError(
                    nameof(modelo.MetodoPago),
                    "Seleccione un método de pago válido.");
            }

            if (!ModelState.IsValid)
            {
                modelo.Subtotal = carrito.Sum(p => p.Subtotal);
                modelo.Descuento = 0;
                modelo.Total = modelo.Subtotal;

                return View(modelo);
            }


            HttpContext.Session.SetString(
                "DatosEntregaCompra",
                JsonSerializer.Serialize(modelo));

            if (modelo.MetodoPago == "Tarjeta de crédito o débito")
            {
                return RedirectToAction(nameof(PagoCompra));
            }

            return RedirectToAction(nameof(ConfirmacionPedido));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            string? datosEntrega = HttpContext.Session
                .GetString("DatosEntregaCompra");

            if (string.IsNullOrWhiteSpace(datosEntrega))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            ProcesoCompraViewModel? entrega;

            try
            {
                entrega = JsonSerializer.Deserialize<ProcesoCompraViewModel>(
                    datosEntrega);
            }
            catch (JsonException)
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            if (entrega == null ||
                (entrega.MetodoPago != "SINPE Móvil" &&
                 entrega.MetodoPago != "Pago al retirar") ||
                (entrega.TipoEntrega != "Retiro" &&
                 entrega.TipoEntrega != "Domicilio") ||
                (entrega.TipoEntrega == "Domicilio" &&
                 string.IsNullOrWhiteSpace(entrega.Direccion)))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var detalles = new List<VentaDetalle>();
                decimal subtotal = 0;

                foreach (var item in carrito)
                {
                    var producto = await _context.Productos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.IdProducto == item.IdProducto);

                    if (producto == null ||
                        producto.TipoPublicacion != "Producto" ||
                        producto.Estado != "Publicado" ||
                        item.Cantidad <= 0 ||
                        producto.StockActual < item.Cantidad)
                    {
                        await transaccion.RollbackAsync();

                        TempData["ErrorCarrito"] =
                            "Uno o más productos no están disponibles.";

                        return RedirectToAction(nameof(Carrito));
                    }

                    bool descontado = await DescontarInventarioAsync(
                        item.IdProducto,
                        item.Cantidad);

                    if (!descontado)
                    {
                        await transaccion.RollbackAsync();

                        TempData["ErrorCarrito"] =
                            "Las existencias cambiaron durante la compra.";

                        return RedirectToAction(nameof(Carrito));
                    }

                    decimal precio = producto.Precio;
                    subtotal += precio * item.Cantidad;

                    detalles.Add(new VentaDetalle
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = precio
                    });
                }

                var venta = new Venta
                {
                    NumeroOrden = "CC-" +
                        Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(),

                    IdUsuario = idUsuario.Value,
                    FechaVenta = DateTime.Now,
                    Subtotal = subtotal,
                    Descuento = 0,
                    Total = subtotal,
                    MetodoPago = entrega.MetodoPago,
                    TipoEntrega = entrega.TipoEntrega,
                    DireccionEntrega = entrega.TipoEntrega == "Domicilio"
                        ? entrega.Direccion
                        : null,
                    Estado = "Pendiente",
                    VentaDetalles = detalles
                };


                _context.Ventas.Add(venta);

                var notificacionComprador = new Notificacione
                {
                    IdUsuario = idUsuario.Value,
                    Titulo = "Pedido registrado",
                    Mensaje = "Tu pedido " + venta.NumeroOrden +
                              " se registró correctamente. " +
                              "El pago está pendiente mediante " +
                              entrega.MetodoPago + ".",
                    Tipo = "Compra",
                    FechaEnvio = DateTime.Now,
                    Leida = false,
                    FechaLectura = null
                };

                _context.Notificaciones.Add(notificacionComprador);

                var idsProductos = detalles
                    .Select(d => d.IdProducto)
                    .Distinct()
                    .ToList();

                var emprendedores = await _context.Productos
                    .AsNoTracking()
                    .Where(p => idsProductos.Contains(p.IdProducto))
                    .Select(p => p.IdEmprendimientoNavigation.IdUsuarioPropietario)
                    .Distinct()
                    .ToListAsync();

                foreach (var idEmprendedor in emprendedores)
                {
                    var notificacionEmprendedor = new Notificacione
                    {
                        IdUsuario = idEmprendedor,
                        Titulo = "Nuevo pedido recibido",
                        Mensaje = "Se ha registrado un pedido de uno o más " +
                                  "de tus productos. Número de pedido: " +
                                  venta.NumeroOrden + ". " +
                                  "Método de pago: " + entrega.MetodoPago + ".",
                        Tipo = "Compra",
                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    };

                    _context.Notificaciones.Add(notificacionEmprendedor);
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();


                HttpContext.Session.Remove("CarritoProductos");
                HttpContext.Session.Remove("DatosEntregaCompra");

                TempData["MensajeCompra"] =
                    "¡Tu pedido se registró correctamente! El pago está pendiente.";

                return RedirectToAction(
                    nameof(DetalleCompra),
                    new { id = venta.NumeroOrden });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorCarrito"] =
                    "No fue posible registrar el pedido. Inténtalo nuevamente.";

                return RedirectToAction(nameof(Carrito));
            }
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmacionPedido()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            string? datosEntrega = HttpContext.Session
                .GetString("DatosEntregaCompra");

            if (string.IsNullOrWhiteSpace(datosEntrega))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            ProcesoCompraViewModel? entrega;

            try
            {
                entrega = JsonSerializer.Deserialize<ProcesoCompraViewModel>(
                    datosEntrega);
            }
            catch (JsonException)
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            if (entrega == null ||
                (entrega.MetodoPago != "SINPE Móvil" &&
                 entrega.MetodoPago != "Pago al retirar"))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            decimal subtotal = 0;

            foreach (var item in carrito)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == item.IdProducto);

                if (producto == null ||
                    producto.TipoPublicacion != "Producto" ||
                    producto.Estado != "Publicado" ||
                    item.Cantidad <= 0 ||
                    producto.StockActual < item.Cantidad)
                {
                    TempData["ErrorCarrito"] =
                        "Uno o más productos no están disponibles.";

                    return RedirectToAction(nameof(Carrito));
                }

                subtotal += producto.Precio * item.Cantidad;
            }

            ViewBag.MetodoPago = entrega.MetodoPago;
            ViewBag.TipoEntrega = entrega.TipoEntrega;
            ViewBag.Total = subtotal;

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> PagoCompra()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] =
                    "El carrito está vacío.";

                return RedirectToAction(nameof(Carrito));
            }

            decimal subtotal = 0;

            foreach (var item in carrito)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == item.IdProducto);

                if (producto == null ||
                    producto.TipoPublicacion != "Producto" ||
                    producto.Estado != "Publicado" ||
                    item.Cantidad <= 0 ||
                    producto.StockActual < item.Cantidad)
                {
                    TempData["ErrorCarrito"] =
                        "Uno o más productos no están disponibles. Revisa tu carrito.";

                    return RedirectToAction(nameof(Carrito));
                }

                subtotal += producto.Precio * item.Cantidad;
            }

            PagoCompraViewModel modelo = new()
            {
                Subtotal = subtotal,
                Descuento = 0,
                TotalPagar = subtotal
            };

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagoCompra(
            PagoCompraViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool usuarioActivo = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo");

            if (!usuarioActivo)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            string? datosEntrega = HttpContext.Session
                .GetString("DatosEntregaCompra");

            if (string.IsNullOrWhiteSpace(datosEntrega))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["ErrorCarrito"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            decimal subtotal = 0;

            foreach (var item in carrito)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == item.IdProducto);

                if (producto == null ||
                    producto.TipoPublicacion != "Producto" ||
                    producto.Estado != "Publicado" ||
                    item.Cantidad <= 0 ||
                    producto.StockActual < item.Cantidad)
                {
                    TempData["ErrorCarrito"] =
                        "Uno o más productos no están disponibles.";

                    return RedirectToAction(nameof(Carrito));
                }

                subtotal += producto.Precio * item.Cantidad;
            }

            modelo.Subtotal = subtotal;
            modelo.Descuento = 0;
            modelo.TotalPagar = subtotal;

            if (modelo.MesVencimiento.HasValue &&
                modelo.AnioVencimiento.HasValue)
            {
                int mes = modelo.MesVencimiento.Value;
                int anio = modelo.AnioVencimiento.Value;

                if (anio < DateTime.Today.Year ||
                    (anio == DateTime.Today.Year &&
                     mes < DateTime.Today.Month))
                {
                    ModelState.AddModelError(
                        nameof(modelo.AnioVencimiento),
                        "La tarjeta está vencida.");
                }
            }

            if (!ModelState.IsValid)
            {
                modelo.NumeroTarjeta = string.Empty;
                modelo.Cvv = string.Empty;
                return View(modelo);
            }

            ResultadoPago resultadoPago = _pagoService.ProcesarPago(
                modelo.NumeroTarjeta,
                modelo.TotalPagar);

            if (!resultadoPago.Aprobado)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultadoPago.Mensaje);

                modelo.NumeroTarjeta = string.Empty;
                modelo.Cvv = string.Empty;

                return View(modelo);
            }

            ProcesoCompraViewModel? entrega;

            try
            {
                entrega = JsonSerializer.Deserialize<ProcesoCompraViewModel>(
                    datosEntrega);
            }
            catch (JsonException)
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            if (entrega == null ||
                (entrega.TipoEntrega != "Retiro" &&
                 entrega.TipoEntrega != "Domicilio") ||
                (entrega.TipoEntrega == "Domicilio" &&
                 string.IsNullOrWhiteSpace(entrega.Direccion)))
            {
                return RedirectToAction(nameof(ProcesoCompra));
            }

            if (entrega.MetodoPago != "Tarjeta de crédito o débito")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Para continuar con esta pasarela, seleccione pago con tarjeta.");

                modelo.NumeroTarjeta = string.Empty;
                modelo.Cvv = string.Empty;

                return View(modelo);
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var detalles = new List<VentaDetalle>();
                decimal subtotalVenta = 0;

                foreach (var item in carrito)
                {
                    var producto = await _context.Productos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.IdProducto == item.IdProducto);

                    if (producto == null ||
                        producto.TipoPublicacion != "Producto" ||
                        producto.Estado != "Publicado" ||
                        item.Cantidad <= 0 ||
                        producto.StockActual < item.Cantidad)
                    {
                        await transaccion.RollbackAsync();

                        TempData["ErrorCarrito"] =
                            "Uno o más productos no tienen existencias suficientes.";

                        return RedirectToAction(nameof(Carrito));
                    }

                    bool descontado = await DescontarInventarioAsync(
                        item.IdProducto,
                        item.Cantidad);

                    if (!descontado)
                    {
                        await transaccion.RollbackAsync();

                        TempData["ErrorCarrito"] =
                            "Las existencias cambiaron durante la compra.";

                        return RedirectToAction(nameof(Carrito));
                    }

                    decimal precio = producto.Precio;
                    subtotalVenta += precio * item.Cantidad;

                    detalles.Add(new VentaDetalle
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = precio
                    });
                }

                if (subtotalVenta != modelo.TotalPagar)
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorCarrito"] =
                        "El total de la compra cambió. Revisa los productos e intenta nuevamente.";

                    return RedirectToAction(nameof(Carrito));
                }

                var configuracionPuntos = await _context.ConfiguracionPuntos
    .AsNoTracking()
    .OrderByDescending(c => c.FechaActualizacion)
    .FirstOrDefaultAsync();

                if (configuracionPuntos == null ||
                    configuracionPuntos.ColonesPorPunto <= 0)
                {
                    throw new InvalidOperationException(
                        "No existe una configuración válida para los puntos.");
                }

                int puntosGanados = (int)Math.Floor(
                    subtotalVenta / configuracionPuntos.ColonesPorPunto);

                var venta = new Venta
                {
                    NumeroOrden = "CC-" +
                        Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(),

                    IdUsuario = idUsuario.Value,
                    FechaVenta = DateTime.Now,
                    Subtotal = subtotalVenta,
                    Descuento = 0,
                    Total = subtotalVenta,
                    MetodoPago = entrega.MetodoPago,
                    TipoEntrega = entrega.TipoEntrega,
                    DireccionEntrega = entrega.TipoEntrega == "Domicilio"
                        ? entrega.Direccion
                        : null,
                    Estado = "Pendiente",
                    EstadoPago = "Aprobado",
                    VentaDetalles = detalles
                };

                _context.Ventas.Add(venta);

                if (puntosGanados > 0)
                {
                    var movimientoPuntos = new MovimientosPunto
                    {
                        IdUsuario = idUsuario.Value,
                        TipoMovimiento = "Acumulacion",
                        Puntos = puntosGanados,
                        Motivo = "Puntos ganados por la compra " + venta.NumeroOrden,
                        FechaMovimiento = DateTime.Now,
                        IdVentaNavigation = venta
                    };

                    _context.MovimientosPuntos.Add(movimientoPuntos);
                }

                var notificacionComprador = new Notificacione
                {
                    IdUsuario = idUsuario.Value,
                    Titulo = "Compra registrada",
                    Mensaje = "Tu pedido " + venta.NumeroOrden +
                              " se registró correctamente. " +
                              "Puedes consultar los detalles de tu compra.",
                    Tipo = "Compra",
                    FechaEnvio = DateTime.Now,
                    Leida = false,
                    FechaLectura = null
                };

                _context.Notificaciones.Add(notificacionComprador);


                var idsProductos = detalles
                    .Select(d => d.IdProducto)
                    .Distinct()
                    .ToList();

                var emprendedores = await _context.Productos
                    .AsNoTracking()
                    .Where(p => idsProductos.Contains(p.IdProducto))
                    .Select(p => p.IdEmprendimientoNavigation.IdUsuarioPropietario)
                    .Distinct()
                    .ToListAsync();

                foreach (var idEmprendedor in emprendedores)
                {
                    var notificacionEmprendedor = new Notificacione
                    {
                        IdUsuario = idEmprendedor,
                        Titulo = "Nueva venta registrada",
                        Mensaje = "Se ha registrado una compra de uno o más de tus productos. " +
                                  "Número de pedido: " + venta.NumeroOrden + ".",
                        Tipo = "Compra",
                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    };

                    _context.Notificaciones.Add(notificacionEmprendedor);
                }


                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();


                HttpContext.Session.Remove("CarritoProductos");
                HttpContext.Session.Remove("DatosEntregaCompra");

                TempData["MensajeCompra"] =
    "¡Pago aprobado! Tu compra se registró correctamente.";

                return RedirectToAction(
                    nameof(DetalleCompra),
                    new { id = venta.NumeroOrden });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorCarrito"] =
                    "No fue posible registrar la compra. Inténtalo nuevamente.";

                return RedirectToAction(nameof(Carrito));
            }

        }


        private static readonly System.Globalization.CultureInfo CulturaCr = new("es-CR");

        private static string FormatoFecha(DateTime fecha) =>
            fecha.ToString("dddd d 'de' MMMM 'de' yyyy", CulturaCr);

        private static string FormatoHorario(DateTime inicio, DateTime? fin) =>
            fin.HasValue && fin.Value > inicio
                ? $"{inicio.ToString("h:mm tt", CulturaCr)} a {fin.Value.ToString("h:mm tt", CulturaCr)}"
                : inicio.ToString("h:mm tt", CulturaCr);

        private async Task<(int? IdUsuario, IActionResult? Redireccion, string? Error)> ValidarSesionRegistroAsync()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return (null, RedirectToAction("IniciarSesion", "Cuenta"), null);
            }

            if (HttpContext.Session.GetString("RolUsuario") == "Administrador")
            {
                return (null, null, "Las cuentas de administración no pueden registrarse en actividades.");
            }

            bool activo = await _context.Usuarios.AnyAsync(u =>
                u.IdUsuario == idUsuario.Value && u.Estado == "Activo");

            if (!activo)
            {
                HttpContext.Session.Clear();

                return (null, RedirectToAction("IniciarSesion", "Cuenta"), null);
            }

            return (idUsuario, null, null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarseEvento(int id)
        {
            var (idUsuario, redireccion, errorSesion) = await ValidarSesionRegistroAsync();

            if (redireccion is not null)
            {
                return redireccion;
            }

            if (errorSesion is not null)
            {
                TempData["ErrorEvento"] = errorSesion;

                return RedirectToAction("DetalleEvento", "Home", new { id });
            }

            await using var transaccion = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            DateTime ahora = DateTime.Now;

            Evento? evento = await _context.Eventos.FirstOrDefaultAsync(e =>
                e.IdEvento == id &&
                (e.Estado == "Programado" || e.Estado == "Activo") &&
                (e.FechaFin ?? e.FechaInicio) >= ahora);

            if (evento is null)
            {
                TempData["ErrorEvento"] = "El evento ya no está disponible.";

                return RedirectToAction("Eventos", "Home");
            }

            InscripcionesEvento? existente = await _context.InscripcionesEventos
                .FirstOrDefaultAsync(i =>
                    i.IdEvento == id && i.IdUsuario == idUsuario!.Value);

            if (existente is not null && ReglasTalleres.InscripcionActiva(existente.Estado))
            {
                TempData["ErrorEvento"] = "Ya estás registrado en este evento.";

                return RedirectToAction("DetalleEvento", "Home", new { id });
            }

            if (evento.CupoMaximo.HasValue)
            {
                int ocupados = await _context.InscripcionesEventos
                    .CountAsync(i => i.IdEvento == id && i.Estado != "Cancelada");

                if (ocupados >= evento.CupoMaximo.Value)
                {
                    TempData["ErrorEvento"] = "Cupo lleno";

                    return RedirectToAction("DetalleEvento", "Home", new { id });
                }
            }

            if (existente is null)
            {
                _context.InscripcionesEventos.Add(new InscripcionesEvento
                {
                    IdEvento = id,
                    IdUsuario = idUsuario!.Value,
                    FechaInscripcion = ahora,
                    Estado = ReglasTalleres.InscripcionConfirmada
                });
            }
            else
            {
                existente.Estado = ReglasTalleres.InscripcionConfirmada;
                existente.FechaInscripcion = ahora;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return RedirectToAction(nameof(ConfirmacionInscripcion), new { tipo = "Evento", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarseTaller(int id)
        {
            var (idUsuario, redireccion, errorSesion) = await ValidarSesionRegistroAsync();

            if (redireccion is not null)
            {
                return redireccion;
            }

            if (errorSesion is not null)
            {
                TempData["ErrorTaller"] = errorSesion;

                return RedirectToAction("DetalleTaller", "Home", new { id });
            }

            await using var transaccion = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            DateTime ahora = DateTime.Now;

            Tallere? taller = await _context.Talleres.FirstOrDefaultAsync(t =>
                t.IdTaller == id &&
                t.Estado == ReglasTalleres.Programado &&
                t.FechaInicio != null &&
                (t.FechaFin ?? t.FechaInicio) >= ahora);

            if (taller is null)
            {
                TempData["ErrorTaller"] = "El taller ya no está disponible.";

                return RedirectToAction("Talleres", "Home");
            }

            InscripcionesTaller? existente = await _context.InscripcionesTallers
                .FirstOrDefaultAsync(i =>
                    i.IdTaller == id && i.IdUsuario == idUsuario!.Value);

            if (existente is not null && ReglasTalleres.InscripcionActiva(existente.Estado))
            {
                TempData["ErrorTaller"] = "Ya estás registrado en este taller.";

                return RedirectToAction("DetalleTaller", "Home", new { id });
            }

            int ocupados = await _context.InscripcionesTallers
                .CountAsync(i => i.IdTaller == id && i.Estado != "Cancelada");

            if (ocupados >= taller.CupoMaximo)
            {
                TempData["ErrorTaller"] = "Cupo completo";

                return RedirectToAction("DetalleTaller", "Home", new { id });
            }

            if (existente is null)
            {
                _context.InscripcionesTallers.Add(new InscripcionesTaller
                {
                    IdTaller = id,
                    IdUsuario = idUsuario!.Value,
                    FechaInscripcion = ahora,
                    Estado = ReglasTalleres.InscripcionConfirmada
                });
            }
            else
            {
                existente.Estado = ReglasTalleres.InscripcionConfirmada;
                existente.FechaInscripcion = ahora;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return RedirectToAction(nameof(ConfirmacionInscripcion), new { tipo = "Taller", id });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmacionInscripcion(string tipo, int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (tipo == "Evento")
            {
                Evento? evento = await _context.Eventos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdEvento == id &&
                        e.InscripcionesEventos.Any(i =>
                            i.IdUsuario == idUsuario.Value && i.Estado != "Cancelada"));

                if (evento is null)
                {
                    return RedirectToAction(nameof(MisInscripciones));
                }

                return View(new ConfirmacionInscripcionViewModel
                {
                    Tipo = "Evento",
                    IdActividad = evento.IdEvento,
                    Nombre = evento.Nombre,
                    Fecha = FormatoFecha(evento.FechaInicio),
                    Horario = FormatoHorario(evento.FechaInicio, evento.FechaFin),
                    Lugar = evento.Ubicacion
                });
            }

            Tallere? taller = await _context.Talleres
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.IdTaller == id &&
                    t.FechaInicio != null &&
                    t.InscripcionesTallers.Any(i =>
                        i.IdUsuario == idUsuario.Value && i.Estado != "Cancelada"));

            if (taller is null)
            {
                return RedirectToAction(nameof(MisTalleres));
            }

            return View(new ConfirmacionInscripcionViewModel
            {
                Tipo = "Taller",
                IdActividad = taller.IdTaller,
                Nombre = taller.Nombre,
                Fecha = FormatoFecha(taller.FechaInicio!.Value),
                Horario = FormatoHorario(taller.FechaInicio.Value, taller.FechaFin),
                Lugar = taller.Ubicacion ?? string.Empty,
                ResponsableNombre = taller.ResponsableNombre,
                ResponsableCorreo = taller.ResponsableCorreo,
                ResponsableTelefono = taller.ResponsableTelefono
            });
        }

        private static string EstadoActividad(string estadoActividad, DateTime inicio, DateTime? fin, DateTime ahora)
        {
            if (estadoActividad == ReglasTalleres.Cancelado)
            {
                return "Cancelado";
            }

            if (estadoActividad == ReglasTalleres.Finalizado || (fin ?? inicio) < ahora)
            {
                return "Finalizado";
            }

            return "Próximo";
        }

        [HttpGet]
        public async Task<IActionResult> MisTalleres(string filtro = "Todos")
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var filas = await _context.InscripcionesTallers
                .AsNoTracking()
                .Where(i =>
                    i.IdUsuario == idUsuario.Value &&
                    i.Estado != "Cancelada" &&
                    i.IdTallerNavigation.FechaInicio != null)
                .Select(i => new
                {
                    i.IdTaller,
                    i.IdTallerNavigation.Nombre,
                    FechaInicio = i.IdTallerNavigation.FechaInicio!.Value,
                    i.IdTallerNavigation.FechaFin,
                    i.IdTallerNavigation.Ubicacion,
                    i.IdTallerNavigation.ImagenUrl,
                    i.IdTallerNavigation.ResponsableNombre,
                    EstadoTaller = i.IdTallerNavigation.Estado
                })
                .ToListAsync();

            DateTime ahora = DateTime.Now;

            List<MiTallerViewModel> todos = filas
                .Select(f => new MiTallerViewModel
                {
                    IdTaller = f.IdTaller,
                    Nombre = f.Nombre,
                    FechaInicio = f.FechaInicio,
                    Fecha = FormatoFecha(f.FechaInicio),
                    Horario = FormatoHorario(f.FechaInicio, f.FechaFin),
                    Ubicacion = f.Ubicacion ?? string.Empty,
                    ImagenUrl = f.ImagenUrl,
                    ResponsableNombre = f.ResponsableNombre,
                    Estado = EstadoActividad(f.EstadoTaller, f.FechaInicio, f.FechaFin, ahora)
                })
                .ToList();

            IEnumerable<MiTallerViewModel> filtrados = filtro switch
            {
                "Proximos" => todos.Where(t => t.Estado == "Próximo").OrderBy(t => t.FechaInicio),
                "Finalizados" => todos.Where(t => t.Estado == "Finalizado").OrderByDescending(t => t.FechaInicio),
                _ => todos.OrderBy(t => t.Estado == "Próximo" ? 0 : 1).ThenBy(t => t.FechaInicio)
            };

            return View(new MisTalleresViewModel
            {
                Filtro = filtro is "Proximos" or "Finalizados" ? filtro : "Todos",
                Total = todos.Count,
                Talleres = filtrados.ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarCancelacionTaller(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Tallere? taller = await ObtenerTallerCancelableAsync(id, idUsuario.Value);

            if (taller is null)
            {
                TempData["ErrorTaller"] = "Este registro ya no se puede cancelar.";

                return RedirectToAction(nameof(MisTalleres));
            }

            DateTime inicio = taller.FechaInicio!.Value;

            return View(new CancelarRegistroTallerViewModel
            {
                IdTaller = taller.IdTaller,
                Nombre = taller.Nombre,
                Fecha = FormatoFecha(inicio),
                Horario = FormatoHorario(inicio, taller.FechaFin),
                Ubicacion = taller.Ubicacion ?? string.Empty,
                MenosDe24Horas = (inicio - DateTime.Now).TotalHours < ReglasTalleres.HorasLimiteCancelacion
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarRegistroTaller(int id, bool confirmar = false)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (!confirmar)
            {
                return RedirectToAction(nameof(ConfirmarCancelacionTaller), new { id });
            }

            Tallere? taller = await ObtenerTallerCancelableAsync(id, idUsuario.Value);

            if (taller is null)
            {
                TempData["ErrorTaller"] = "Este registro ya no se puede cancelar.";

                return RedirectToAction(nameof(MisTalleres));
            }

            InscripcionesTaller inscripcion = await _context.InscripcionesTallers
                .FirstAsync(i =>
                    i.IdTaller == id &&
                    i.IdUsuario == idUsuario.Value &&
                    i.Estado != "Cancelada");

            inscripcion.Estado = ReglasTalleres.InscripcionCancelada;

            await _context.SaveChangesAsync();

            TempData["MensajeTaller"] =
                $"Cancelaste tu registro en «{taller.Nombre}». El cupo quedó disponible para otras personas.";

            return RedirectToAction(nameof(MisTalleres));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarTallerCancelado(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            InscripcionesTaller? inscripcion = await _context.InscripcionesTallers
                .Include(i => i.IdTallerNavigation)
                .FirstOrDefaultAsync(i =>
                    i.IdTaller == id &&
                    i.IdUsuario == idUsuario.Value &&
                    i.IdTallerNavigation.Estado == ReglasTalleres.Cancelado);

            if (inscripcion is null)
            {
                TempData["ErrorTaller"] = "El taller no está disponible para eliminarlo.";

                return RedirectToAction(nameof(MisTalleres));
            }

            _context.InscripcionesTallers.Remove(inscripcion);
            await _context.SaveChangesAsync();

            TempData["MensajeTaller"] =
                $"El taller cancelado «{inscripcion.IdTallerNavigation.Nombre}» fue eliminado de tus talleres.";

            return RedirectToAction(nameof(MisTalleres));
        }

        private async Task<Tallere?> ObtenerTallerCancelableAsync(int idTaller, int idUsuario)
        {
            DateTime ahora = DateTime.Now;

            return await _context.Talleres
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.IdTaller == idTaller &&
                    t.Estado == ReglasTalleres.Programado &&
                    t.FechaInicio != null &&
                    t.FechaInicio > ahora &&
                    t.InscripcionesTallers.Any(i =>
                        i.IdUsuario == idUsuario && i.Estado != "Cancelada"));
        }

        [HttpGet]
        public async Task<IActionResult> MisInscripciones()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            DateTime ahora = DateTime.Now;

            var eventos = await _context.InscripcionesEventos
                .AsNoTracking()
                .Where(i => i.IdUsuario == idUsuario.Value)
                .Select(i => new
                {
                    i.IdEvento,
                    i.IdEventoNavigation.Nombre,
                    i.IdEventoNavigation.FechaInicio,
                    i.IdEventoNavigation.FechaFin,
                    i.IdEventoNavigation.Ubicacion,
                    i.IdEventoNavigation.ImagenUrl,
                    EstadoActividad = i.IdEventoNavigation.Estado,
                    EstadoInscripcion = i.Estado
                })
                .ToListAsync();

            var talleres = await _context.InscripcionesTallers
                .AsNoTracking()
                .Where(i =>
                    i.IdUsuario == idUsuario.Value &&
                    i.IdTallerNavigation.FechaInicio != null)
                .Select(i => new
                {
                    i.IdTaller,
                    i.IdTallerNavigation.Nombre,
                    FechaInicio = i.IdTallerNavigation.FechaInicio!.Value,
                    i.IdTallerNavigation.FechaFin,
                    i.IdTallerNavigation.Ubicacion,
                    i.IdTallerNavigation.ImagenUrl,
                    EstadoActividad = i.IdTallerNavigation.Estado,
                    EstadoInscripcion = i.Estado
                })
                .ToListAsync();

            List<(DateTime Orden, InscripcionUsuarioViewModel Item)> lista = [];

            foreach (var e in eventos)
            {
                lista.Add((e.FechaInicio, new InscripcionUsuarioViewModel
                {
                    Id = e.IdEvento,
                    Tipo = "Evento",
                    Nombre = e.Nombre,
                    Fecha = e.FechaInicio.ToString("d 'de' MMMM, h:mm tt", CulturaCr),
                    Ubicacion = e.Ubicacion,
                    Estado = e.EstadoInscripcion == "Cancelada"
                        ? "Cancelado"
                        : EstadoActividad(e.EstadoActividad, e.FechaInicio, e.FechaFin, ahora),
                    Imagen = string.IsNullOrWhiteSpace(e.ImagenUrl) ? "/images/hero-feria.jpg" : e.ImagenUrl
                }));
            }

            foreach (var t in talleres)
            {
                lista.Add((t.FechaInicio, new InscripcionUsuarioViewModel
                {
                    Id = t.IdTaller,
                    Tipo = "Taller",
                    Nombre = t.Nombre,
                    Fecha = t.FechaInicio.ToString("d 'de' MMMM, h:mm tt", CulturaCr),
                    Ubicacion = t.Ubicacion ?? string.Empty,
                    Estado = t.EstadoInscripcion == "Cancelada"
                        ? "Cancelado"
                        : EstadoActividad(t.EstadoActividad, t.FechaInicio, t.FechaFin, ahora),
                    Imagen = string.IsNullOrWhiteSpace(t.ImagenUrl) ? "/images/taller-ceramica.jpg" : t.ImagenUrl
                }));
            }

            return View(lista
                .OrderByDescending(x => x.Orden)
                .Select(x => x.Item)
                .ToList());
        }


        [HttpGet]
        public async Task<IActionResult> PuntosRecompensas()
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

            var movimientos = await _context.MovimientosPuntos
                .AsNoTracking()
                .Where(m => m.IdUsuario == idUsuario.Value)
                .OrderByDescending(m => m.FechaMovimiento)
                .ThenByDescending(m => m.IdMovimientoPuntos)
                .Select(m => new MovimientoPuntosViewModel
                {
                    Fecha = m.FechaMovimiento,
                    Descripcion = m.Motivo,
                    Puntos = m.Puntos,
                    Tipo = m.TipoMovimiento
                })
                .ToListAsync();

            int saldoPuntos = movimientos.Sum(m => m.Puntos);

            PuntosRecompensasViewModel modelo = new()
            {
                NombreUsuario = usuario.Nombre,
                SaldoPuntos = saldoPuntos,
                CodigoCliente = $"CC-{usuario.IdUsuario}",

                Movimientos = movimientos,

                Recompensas =
                [
                    new()
            {
                Id = 1,
                Nombre = "10 % de descuento",
                Descripcion = "Aplicable en una compra seleccionada.",
                CostoPuntos = 500,
                Icono = "bi-percent",
                Disponible = saldoPuntos >= 500
            },
            new()
            {
                Id = 2,
                Nombre = "Entrada especial",
                Descripcion = "Acceso prioritario a una feria.",
                CostoPuntos = 900,
                Icono = "bi-ticket-perforated-fill",
                Disponible = saldoPuntos >= 900
            },
            new()
            {
                Id = 3,
                Nombre = "Taller gratuito",
                Descripcion = "Canje por un taller participante.",
                CostoPuntos = 1500,
                Icono = "bi-palette-fill",
                Disponible = saldoPuntos >= 1500
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
        public async Task<IActionResult> Notificaciones()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("InicioSesion", "Cuenta");
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => n.IdUsuario == idUsuario.Value)
                .OrderByDescending(n => n.FechaEnvio)
                .Select(n => new NotificacionListadoViewModel
                {
                    Id = (int)n.IdNotificacion,
                    Tipo = n.Tipo,
                    Titulo = n.Titulo,
                    Mensaje = n.Mensaje,
                    Fecha = n.FechaEnvio.ToString("dd/MM/yyyy HH:mm"),
                    Icono = n.Tipo == "Compra"
                        ? "bi-bag-check-fill"
                        : n.Tipo == "Evento"
                            ? "bi-calendar-check-fill"
                            : n.Tipo == "Puntos"
                                ? "bi-star-fill"
                                : n.Tipo == "Promoción"
                                    ? "bi-megaphone-fill"
                                    : "bi-bell-fill",
                    Leida = n.Leida
                })
                .ToListAsync();

            return View(notificaciones);
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
