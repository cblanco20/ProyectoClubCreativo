using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels.Admin;

namespace ProyectoClubCreativo.Controllers
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _entornoWeb;

        public AdminController(IWebHostEnvironment entornoWeb)
        {
            _entornoWeb = entornoWeb;
        }

        public IActionResult CerrarSesion()
        {
            return RedirectToAction("IniciarSesion", "Cuenta");
        }

        // ---------- DASHBOARD ----------
        [HttpGet]
        public IActionResult Panel(FiltroDashboardViewModel filtro)
        {
            DashboardAdminViewModel modelo = new()
            {
                Filtro = filtro,
                TotalUsuarios = 486,
                EmprendimientosRegistrados = 57,
                SolicitudesPendientes = 8,
                ProductosActivos = 312,
                VentasTotal = 4820500,
                VariacionVentasPorcentaje = 12,

                VentasPorMes =
                [
                    new() { Etiqueta = "Mar", Valor = 45 },
                    new() { Etiqueta = "Abr", Valor = 58 },
                    new() { Etiqueta = "May", Valor = 50 },
                    new() { Etiqueta = "Jun", Valor = 72 },
                    new() { Etiqueta = "Jul", Valor = 65 },
                    new() { Etiqueta = "Ago", Valor = 90 }
                ],

                ProductosPorCategoria =
                [
                    new() { Etiqueta = "Arte e ilustración", Valor = 34 },
                    new() { Etiqueta = "Accesorios", Valor = 26 },
                    new() { Etiqueta = "Hogar y decoración", Valor = 20 },
                    new() { Etiqueta = "Velas y aromas", Valor = 12 },
                    new() { Etiqueta = "Otros", Valor = 8 }
                ],

                ProximosEventos =
                [
                    new() { Nombre = "Arte Inarrivo San Pedro", Fecha = "15 y 16 de agosto", Ubicacion = "San Pedro", Inscritos = 120 },
                    new() { Nombre = "Feria Creativa Santa Ana", Fecha = "5 al 7 de septiembre", Ubicacion = "Santa Ana", Inscritos = 86 },
                    new() { Nombre = "Feria Creativa Heredia", Fecha = "20 de septiembre", Ubicacion = "Heredia", Inscritos = 40 }
                ],

                TalleresDisponibles =
                [
                    new() { Nombre = "Cerámica para principiantes", Fecha = "23 de agosto", CuposDisponibles = 4, CuposTotales = 15 },
                    new() { Nombre = "Bordado creativo", Fecha = "30 de agosto", CuposDisponibles = 9, CuposTotales = 12 },
                    new() { Nombre = "Introducción al macramé", Fecha = "6 de septiembre", CuposDisponibles = 12, CuposTotales = 12 }
                ],

                Alertas =
                [
                    new() { Tipo = "Solicitudes", Mensaje = "Hay 8 solicitudes de emprendimiento pendientes de revisión.", Fecha = "Hoy", Icono = "bi-file-earmark-text-fill", Nivel = "advertencia" },
                    new() { Tipo = "Suscripciones", Mensaje = "3 suscripciones vencen esta semana.", Fecha = "Hoy", Icono = "bi-credit-card-2-front-fill", Nivel = "advertencia" },
                    new() { Tipo = "Productos", Mensaje = "2 productos fueron reportados por contenido inapropiado.", Fecha = "Ayer", Icono = "bi-flag-fill", Nivel = "critica" },
                    new() { Tipo = "Cupos", Mensaje = "El taller \"Introducción al macramé\" alcanzó cupo lleno.", Fecha = "Ayer", Icono = "bi-people-fill", Nivel = "informativa" }
                ],

                ActividadReciente =
                [
                    new() { Mensaje = "Se aprobó el emprendimiento \"Luz Natural\".", Fecha = "Hoy, 10:20 a. m.", Icono = "bi-patch-check-fill" },
                    new() { Mensaje = "Nuevo usuario registrado: Carla Ramírez.", Fecha = "Hoy, 9:05 a. m.", Icono = "bi-person-plus-fill" },
                    new() { Mensaje = "Se creó el plan de suscripción \"Plan Premium\".", Fecha = "Ayer, 4:40 p. m.", Icono = "bi-gem" },
                    new() { Mensaje = "Producto \"Aretes Orquídea\" marcado como revisado.", Fecha = "Ayer, 2:15 p. m.", Icono = "bi-box-seam-fill" }
                ]
            };

            return View(modelo);
        }

        // ---------- USUARIOS ----------
        [HttpGet]
        public IActionResult Usuarios(string? busqueda, string? rol)
        {
            List<UsuarioAdminViewModel> usuarios = ObtenerUsuariosDemo();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                usuarios = usuarios
                    .Where(u =>
                        u.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        u.Correo.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(rol) && rol != "Todos")
            {
                usuarios = usuarios.Where(u => u.Rol == rol).ToList();
            }

            ListadoUsuariosAdminViewModel modelo = new()
            {
                Busqueda = busqueda,
                RolFiltro = rol,
                Usuarios = usuarios
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult PerfilUsuario(int id = 1)
        {
            UsuarioAdminViewModel usuario =
                ObtenerUsuariosDemo().FirstOrDefault(u => u.Id == id)
                ?? ObtenerUsuariosDemo().First();

            PerfilUsuarioAdminViewModel modelo = new()
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre.Split(' ')[0],
                Apellidos = string.Join(' ', usuario.Nombre.Split(' ').Skip(1)),
                Correo = usuario.Correo,
                Telefono = "8888-0000",
                Provincia = "San José",
                Rol = usuario.Rol,
                Estado = usuario.Estado,
                FechaRegistro = usuario.FechaRegistro,
                Foto = usuario.Foto,
                ComprasRealizadas = 6,
                PuntosAcumulados = 1280
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PerfilUsuario(PerfilUsuarioAdminViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeAdmin"] =
                $"Los datos de {modelo.Nombre} se actualizaron correctamente (rol: {modelo.Rol}, estado: {modelo.Estado}).";

            return RedirectToAction(nameof(PerfilUsuario), new { id = modelo.Id });
        }

        private static List<UsuarioAdminViewModel> ObtenerUsuariosDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Maria Montero Cruz", Correo = "maria@ejemplo.com", Rol = "Usuario", Estado = "Activo", FechaRegistro = "12/01/2026" },
                new() { Id = 2, Nombre = "Carla Ramírez Solís", Correo = "carla@ejemplo.com", Rol = "Usuario", Estado = "Activo", FechaRegistro = "03/08/2026" },
                new() { Id = 3, Nombre = "Valentín Arce Mora", Correo = "valentin@gmail.com", Rol = "Emprendedor", Estado = "Activo", FechaRegistro = "20/05/2026" },
                new() { Id = 4, Nombre = "Andrea Castro Vega", Correo = "andrea@ejemplo.com", Rol = "Emprendedor", Estado = "Inactivo", FechaRegistro = "14/03/2026" },
                new() { Id = 5, Nombre = "Jossete Sánchez", Correo = "jossete.sanchez@clubcreativomivo.com", Rol = "Administrador", Estado = "Activo", FechaRegistro = "01/01/2026" },
                new() { Id = 6, Nombre = "Luis Fernández Rojas", Correo = "luis@ejemplo.com", Rol = "Usuario", Estado = "Activo", FechaRegistro = "28/07/2026" }
            ];
        }

        // ---------- EMPRENDIMIENTOS ----------
        [HttpGet]
        public IActionResult Emprendimientos(string estado = "Pendiente")
        {
            List<SolicitudEmprendimientoAdminViewModel> solicitudes = ObtenerSolicitudesDemo();

            ListadoEmprendimientosAdminViewModel modelo = new()
            {
                FiltroEstado = estado,
                Solicitudes = solicitudes.Where(s => s.Estado == estado).ToList(),
                TotalPendientes = solicitudes.Count(s => s.Estado == "Pendiente"),
                TotalAprobados = solicitudes.Count(s => s.Estado == "Aprobado"),
                TotalRechazados = solicitudes.Count(s => s.Estado == "Rechazado")
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult DetalleSolicitud(int id = 1)
        {
            SolicitudEmprendimientoAdminViewModel modelo =
                ObtenerSolicitudesDemo().FirstOrDefault(s => s.Id == id)
                ?? ObtenerSolicitudesDemo().First();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Aprobar(int id)
        {
            TempData["MensajeAdmin"] = "La solicitud fue aprobada correctamente.";
            return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rechazar(RechazarSolicitudViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                SolicitudEmprendimientoAdminViewModel solicitud =
                    ObtenerSolicitudesDemo().FirstOrDefault(s => s.Id == modelo.Id)
                    ?? ObtenerSolicitudesDemo().First();

                solicitud.MotivoRechazo = modelo.Motivo;

                return View(nameof(DetalleSolicitud), solicitud);
            }

            TempData["MensajeAdmin"] = "La solicitud fue rechazada y se notificó el motivo al solicitante.";
            return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
        }

        private static List<SolicitudEmprendimientoAdminViewModel> ObtenerSolicitudesDemo()
        {
            return
            [
                new()
                {
                    Id = 1,
                    NombreComercial = "Artesanías MiVo",
                    Solicitante = "Valentín Arce Mora",
                    Categoria = "Arte e ilustración",
                    Correo = "valentin@gmail.com",
                    Telefono = "8888-7777",
                    FechaSolicitud = "01/08/2026",
                    Estado = "Pendiente",
                    Descripcion = "Elaboramos productos artesanales hechos a mano para decoración, obsequios y pedidos personalizados."
                },
                new()
                {
                    Id = 2,
                    NombreComercial = "Luz Natural",
                    Solicitante = "Andrea Castro Vega",
                    Categoria = "Velas y aromas",
                    Correo = "andrea@ejemplo.com",
                    Telefono = "8777-1234",
                    FechaSolicitud = "29/07/2026",
                    Estado = "Pendiente",
                    Descripcion = "Velas artesanales elaboradas con cera de soya y aromas naturales."
                },
                new()
                {
                    Id = 3,
                    NombreComercial = "Orquídea",
                    Solicitante = "Sofía Jiménez",
                    Categoria = "Accesorios",
                    Correo = "sofia@ejemplo.com",
                    Telefono = "8666-4321",
                    FechaSolicitud = "10/07/2026",
                    Estado = "Aprobado",
                    Descripcion = "Aretes y accesorios tejidos a mano."
                },
                new()
                {
                    Id = 4,
                    NombreComercial = "Trazo Libre",
                    Solicitante = "Kevin Alvarado",
                    Categoria = "Arte e ilustración",
                    Correo = "kevin@ejemplo.com",
                    Telefono = "8555-9988",
                    FechaSolicitud = "02/07/2026",
                    Estado = "Rechazado",
                    Descripcion = "Ilustraciones digitales por encargo.",
                    MotivoRechazo = "La información de contacto suministrada no pudo ser verificada."
                }
            ];
        }

        // ---------- SUSCRIPCIONES ----------
        [HttpGet]
        public IActionResult Suscripciones()
        {
            GestionSuscripcionesViewModel modelo = new()
            {
                Planes = ObtenerPlanesDemo(),
                Activas =
                [
                    new() { Id = 1, Emprendimiento = "Artesanías MiVo", Plan = "Plan Emprendedor", FechaInicio = "01/02/2026", FechaVencimiento = "01/09/2026", Estado = "Por vencer" },
                    new() { Id = 2, Emprendimiento = "Orquídea", Plan = "Plan Premium", FechaInicio = "15/01/2026", FechaVencimiento = "15/01/2027", Estado = "Vigente" },
                    new() { Id = 3, Emprendimiento = "Luz Natural", Plan = "Plan Básico", FechaInicio = "20/03/2026", FechaVencimiento = "20/08/2026", Estado = "Por vencer" },
                    new() { Id = 4, Emprendimiento = "Trazo Libre", Plan = "Plan Básico", FechaInicio = "10/12/2025", FechaVencimiento = "10/07/2026", Estado = "Vencida" }
                ]
            };

            modelo.TotalPorVencer = modelo.Activas.Count(a => a.Estado == "Por vencer");

            return View(modelo);
        }

        [HttpGet]
        public IActionResult CrearPlan()
        {
            ViewData["Titulo"] = "Crear plan de suscripción";
            return View("FormularioPlan", new PlanSuscripcionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearPlan(PlanSuscripcionViewModel modelo)
        {
            ViewData["Titulo"] = "Crear plan de suscripción";

            if (!ModelState.IsValid)
            {
                return View("FormularioPlan", modelo);
            }

            TempData["MensajeAdmin"] = $"El plan \"{modelo.Nombre}\" fue creado correctamente.";
            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpGet]
        public IActionResult EditarPlan(int id = 1)
        {
            PlanSuscripcionViewModel modelo =
                ObtenerPlanesDemo().FirstOrDefault(p => p.Id == id)
                ?? new PlanSuscripcionViewModel { Id = id };

            ViewData["Titulo"] = "Editar plan de suscripción";
            return View("FormularioPlan", modelo);
        }

        private static List<PlanSuscripcionViewModel> ObtenerPlanesDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Plan Básico", Precio = 8000, Periodicidad = "Mensual", Beneficios = "1 espacio en catálogo\nParticipación en 1 feria al mes", CantidadSuscriptores = 22, Activo = true },
                new() { Id = 2, Nombre = "Plan Emprendedor", Precio = 15000, Periodicidad = "Mensual", Beneficios = "5 productos destacados\nParticipación en 2 ferias al mes\nEstadísticas básicas", CantidadSuscriptores = 27, Activo = true },
                new() { Id = 3, Nombre = "Plan Premium", Precio = 25000, Periodicidad = "Mensual", Beneficios = "Productos ilimitados\nParticipación en todas las ferias\nEstadísticas avanzadas", CantidadSuscriptores = 8, Activo = true }
            ];
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarPlan(PlanSuscripcionViewModel modelo)
        {
            ViewData["Titulo"] = "Editar plan de suscripción";

            if (!ModelState.IsValid)
            {
                return View("FormularioPlan", modelo);
            }

            TempData["MensajeAdmin"] = $"El plan \"{modelo.Nombre}\" fue actualizado correctamente.";
            return RedirectToAction(nameof(Suscripciones));
        }

        // ---------- PRODUCTOS ----------
        [HttpGet]
        public IActionResult Productos(string? busqueda, string? categoria, string? estado)
        {
            List<ProductoAdminViewModel> productos = ObtenerProductosDemo();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                productos = productos
                    .Where(p =>
                        p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        p.Emprendimiento.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                productos = productos.Where(p => p.Categoria == categoria).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                productos = productos.Where(p => p.Estado == estado).ToList();
            }

            ListadoProductosAdminViewModel modelo = new()
            {
                Busqueda = busqueda,
                FiltroCategoria = categoria,
                FiltroEstado = estado,
                Productos = productos
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult RevisarProducto(int id = 1)
        {
            ProductoAdminViewModel modelo =
                ObtenerProductosDemo().FirstOrDefault(p => p.Id == id)
                ?? ObtenerProductosDemo().First();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstadoProducto(int id, string estado)
        {
            TempData["MensajeAdmin"] = $"El estado del producto se actualizó a \"{estado}\".";
            return RedirectToAction(nameof(RevisarProducto), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int id)
        {
            TempData["MensajeAdmin"] = "El producto fue eliminado por contenido inapropiado.";
            return RedirectToAction(nameof(Productos));
        }

        private static List<ProductoAdminViewModel> ObtenerProductosDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Aretes Orquídea", Emprendimiento = "Orquídea", Categoria = "Accesorios", Precio = 12000, Estado = "Activo", Imagen = "/images/producto-1.jpg", FechaPublicacion = "10/06/2026", Reportes = 0, Descripcion = "Aretes tejidos a mano con hilo encerado." },
                new() { Id = 2, Nombre = "Vela Aromática Lavanda", Emprendimiento = "Luz Natural", Categoria = "Velas y aromas", Precio = 6500, Estado = "Activo", Imagen = "/images/producto-3.jpg", FechaPublicacion = "02/07/2026", Reportes = 0, Descripcion = "Vela de cera de soya con esencia de lavanda." },
                new() { Id = 3, Nombre = "Cuadro decorativo Boho", Emprendimiento = "Artesanías MiVo", Categoria = "Hogar y decoración", Precio = 18500, Estado = "En revisión", Imagen = "/images/producto-1.jpg", FechaPublicacion = "20/07/2026", Reportes = 2, Descripcion = "Cuadro tejido estilo boho para decoración de interiores." },
                new() { Id = 4, Nombre = "Ilustración digital personalizada", Emprendimiento = "Trazo Libre", Categoria = "Arte e ilustración", Precio = 9000, Estado = "Pausado", Imagen = "/images/producto-3.jpg", FechaPublicacion = "15/05/2026", Reportes = 1, Descripcion = "Retrato ilustrado digital por encargo." }
            ];
        }
        [HttpGet]
        public IActionResult Eventos(string estado = "Todos")
        {
            var eventos = ObtenerEventosDemo();
            return View(new ListadoEventosAdminViewModel
            {
                FiltroEstado = estado,
                Eventos = estado == "Todos" ? eventos : eventos.Where(e => e.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearEvento()
        {
            ViewData["Titulo"] = "Crear evento";
            return View("FormularioEvento", new EventoAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearEvento(EventoAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear evento";
            if (!ModelState.IsValid) return View("FormularioEvento", modelo);
            TempData["MensajeAdmin"] = $"El evento \"{modelo.Nombre}\" fue creado correctamente.";
            return RedirectToAction(nameof(Eventos));
        }

        [HttpGet]
        public IActionResult EditarEvento(int id = 1)
        {
            ViewData["Titulo"] = "Editar evento";
            var evento = ObtenerEventosDemo().FirstOrDefault(e => e.Id == id) ?? new EventoAdminViewModel { Id = id };
            return View("FormularioEvento", evento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarEvento(EventoAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar evento";
            if (!ModelState.IsValid) return View("FormularioEvento", modelo);
            TempData["MensajeAdmin"] = $"El evento \"{modelo.Nombre}\" fue actualizado correctamente.";
            return RedirectToAction(nameof(Eventos));
        }

        [HttpGet]
        public IActionResult ParticipantesEvento(int id = 1)
        {
            var evento = ObtenerEventosDemo().FirstOrDefault(e => e.Id == id) ?? ObtenerEventosDemo().First();
            return View(evento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AsignarEmprendimiento(int id, string nombreEmprendimiento)
        {
            TempData["MensajeAdmin"] = $"\"{nombreEmprendimiento}\" fue asignado al evento.";
            return RedirectToAction(nameof(ParticipantesEvento), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarEvento(int id, string motivo)
        {
            TempData["MensajeAdmin"] = $"El evento fue cancelado. Motivo: {motivo}";
            return RedirectToAction(nameof(Eventos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrEvento(int id)
        {
            TempData["MensajeAdmin"] = "El código QR del evento fue generado correctamente.";
            return RedirectToAction(nameof(ParticipantesEvento), new { id });
        }

        private static List<EventoAdminViewModel> ObtenerEventosDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte Inarrivo San Pedro", Fecha = "15 y 16 de agosto", Ubicacion = "San Pedro", CupoTotal = 150, Inscritos = 120, Estado = "Programado", QrGenerado = true,
            EmprendimientosAsignados = ["Orquídea", "Luz Natural"],
            Participantes = [ new() { Nombre = "Maria Montero Cruz", Emprendimiento = "Orquídea", Estado = "Confirmado" }, new() { Nombre = "Carla Ramírez Solís", Emprendimiento = "Luz Natural", Estado = "Pendiente" } ] },
        new() { Id = 2, Nombre = "Feria Creativa Santa Ana", Fecha = "5 al 7 de septiembre", Ubicacion = "Santa Ana", CupoTotal = 100, Inscritos = 86, Estado = "Programado", QrGenerado = false,
            EmprendimientosAsignados = ["Artesanías MiVo"], Participantes = [] },
        new() { Id = 3, Nombre = "Feria Creativa Heredia", Fecha = "20 de septiembre", Ubicacion = "Heredia", CupoTotal = 60, Inscritos = 40, Estado = "Cancelado", QrGenerado = false, Participantes = [] }
            ];
        }
        [HttpGet]
        public IActionResult Talleres(string estado = "Todos")
        {
            var talleres = ObtenerTalleresDemo();
            return View(new ListadoTalleresAdminViewModel
            {
                FiltroEstado = estado,
                Talleres = estado == "Todos" ? talleres : talleres.Where(t => t.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearTaller()
        {
            ViewData["Titulo"] = "Crear taller";
            return View("FormularioTaller", new TallerAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearTaller(TallerAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear taller";
            if (!ModelState.IsValid) return View("FormularioTaller", modelo);
            TempData["MensajeAdmin"] = $"El taller \"{modelo.Nombre}\" fue creado correctamente.";
            return RedirectToAction(nameof(Talleres));
        }

        [HttpGet]
        public IActionResult EditarTaller(int id = 1)
        {
            ViewData["Titulo"] = "Editar taller (cupos y datos)";
            var taller = ObtenerTalleresDemo().FirstOrDefault(t => t.Id == id) ?? new TallerAdminViewModel { Id = id };
            return View("FormularioTaller", taller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarTaller(TallerAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar taller (cupos y datos)";
            if (!ModelState.IsValid) return View("FormularioTaller", modelo);
            TempData["MensajeAdmin"] = $"El taller \"{modelo.Nombre}\" fue actualizado. Cupos disponibles: {modelo.CuposDisponibles}.";
            return RedirectToAction(nameof(Talleres));
        }

        [HttpGet]
        public IActionResult InscritosTaller(int id = 1)
        {
            var taller = ObtenerTalleresDemo().FirstOrDefault(t => t.Id == id) ?? ObtenerTalleresDemo().First();
            return View(taller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarTaller(int id, string motivo)
        {
            TempData["MensajeAdmin"] = $"El taller fue cancelado. Motivo: {motivo}";
            return RedirectToAction(nameof(Talleres));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrTaller(int id)
        {
            TempData["MensajeAdmin"] = "El código QR del taller fue generado correctamente.";
            return RedirectToAction(nameof(InscritosTaller), new { id });
        }

        private static List<TallerAdminViewModel> ObtenerTalleresDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Cerámica para principiantes", Fecha = "23 de agosto", CuposTotales = 15, CuposDisponibles = 4, Estado = "Programado", QrGenerado = true,
            Inscritos = [ new() { Nombre = "Maria Montero Cruz", Correo = "maria@ejemplo.com", Estado = "Confirmado" } ] },
        new() { Id = 2, Nombre = "Bordado creativo", Fecha = "30 de agosto", CuposTotales = 12, CuposDisponibles = 9, Estado = "Programado", QrGenerado = false, Inscritos = [] },
        new() { Id = 3, Nombre = "Introducción al macramé", Fecha = "6 de septiembre", CuposTotales = 12, CuposDisponibles = 0, Estado = "Programado", QrGenerado = false, Inscritos = [] }
            ];
        }
        [HttpGet]
        public IActionResult Ventas(string estado = "Todas")
        {
            List<VentaAdminViewModel> ventas = ObtenerVentasDemo();

            ListadoVentasAdminViewModel modelo = new()
            {
                FiltroEstado = estado,
                Ventas = estado == "Todas" ? ventas : ventas.Where(v => v.Estado == estado).ToList(),
                TotalVendido = ventas.Where(v => v.Estado == "Pagada").Sum(v => v.Total),
                TotalOrdenes = ventas.Count,
                TotalPendientes = ventas.Count(v => v.Estado == "Pendiente"),
                TotalCanceladas = ventas.Count(v => v.Estado == "Cancelada")
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult DetalleVenta(int id = 1)
        {
            VentaAdminViewModel modelo =
                ObtenerVentasDemo().FirstOrDefault(v => v.Id == id)
                ?? ObtenerVentasDemo().First();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarVenta(int id, string motivo)
        {
            TempData["MensajeAdmin"] = $"La orden fue cancelada. Motivo: {motivo}";
            return RedirectToAction(nameof(DetalleVenta), new { id });
        }

        private static List<VentaAdminViewModel> ObtenerVentasDemo()
        {
            return
            [
                new() { Id = 1, NumeroOrden = "ORD-1042", Cliente = "Maria Montero Cruz", Emprendimiento = "Orquídea", Fecha = "02/08/2026", Total = 24000, Estado = "Pagada", MetodoPago = "Sinpe Móvil",
            Items = [ new() { Producto = "Aretes Orquídea", Cantidad = 2, PrecioUnitario = 12000 } ] },
        new() { Id = 2, NumeroOrden = "ORD-1043", Cliente = "Carla Ramírez Solís", Emprendimiento = "Luz Natural", Fecha = "03/08/2026", Total = 13000, Estado = "Pendiente", MetodoPago = "Tarjeta",
            Items = [ new() { Producto = "Vela Aromática Lavanda", Cantidad = 2, PrecioUnitario = 6500 } ] },
        new() { Id = 3, NumeroOrden = "ORD-1044", Cliente = "Luis Fernández Rojas", Emprendimiento = "Artesanías MiVo", Fecha = "01/08/2026", Total = 18500, Estado = "Cancelada", MetodoPago = "Tarjeta",
            Items = [ new() { Producto = "Cuadro decorativo Boho", Cantidad = 1, PrecioUnitario = 18500 } ], MotivoCancelacion = "El cliente solicitó la cancelación por cambio de dirección." },
        new() { Id = 4, NumeroOrden = "ORD-1045", Cliente = "Andrea Castro Vega", Emprendimiento = "Trazo Libre", Fecha = "04/08/2026", Total = 9000, Estado = "Pagada", MetodoPago = "Sinpe Móvil",
            Items = [ new() { Producto = "Ilustración digital personalizada", Cantidad = 1, PrecioUnitario = 9000 } ] }
            ];
        }
        [HttpGet]
        public IActionResult PuntosRecompensas(string pestana = "Recompensas")
        {
            return View(new PuntosRecompensasAdminViewModel
            {
                Pestana = pestana,
                Recompensas = ObtenerRecompensasDemo(),
                Movimientos = ObtenerMovimientosDemo()
            });
        }

        [HttpGet]
        public IActionResult CrearRecompensa()
        {
            ViewData["Titulo"] = "Crear recompensa";
            return View("FormularioRecompensa", new RecompensaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear recompensa";
            if (!ModelState.IsValid) return View("FormularioRecompensa", modelo);
            TempData["MensajeAdmin"] = $"La recompensa \"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpGet]
        public IActionResult EditarRecompensa(int id = 1)
        {
            ViewData["Titulo"] = "Editar recompensa";
            var r = ObtenerRecompensasDemo().FirstOrDefault(x => x.Id == id) ?? new RecompensaAdminViewModel { Id = id };
            return View("FormularioRecompensa", r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar recompensa";
            if (!ModelState.IsValid) return View("FormularioRecompensa", modelo);
            TempData["MensajeAdmin"] = $"La recompensa \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjustarPuntos(AjustarPuntosViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeAdmin"] = "Completá correo, puntos y motivo para ajustar puntos.";
                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            TempData["MensajeAdmin"] = $"Se ajustaron {modelo.Puntos} puntos para {modelo.Correo}. Motivo: {modelo.Motivo}";
            return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
        }

        private static List<RecompensaAdminViewModel> ObtenerRecompensasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Descuento 10% en catálogo", Descripcion = "Aplica a cualquier producto activo.", CostoPuntos = 500, Activa = true, CantidadCanjes = 34 },
        new() { Id = 2, Nombre = "Entrada gratis a un taller", Descripcion = "Un cupo en cualquier taller con disponibilidad.", CostoPuntos = 800, Activa = true, CantidadCanjes = 12 },
        new() { Id = 3, Nombre = "Kit de bienvenida Club Creativo", Descripcion = "Merchandising físico del club.", CostoPuntos = 1200, Activa = false, CantidadCanjes = 5 }
            ];
        }

        private static List<MovimientoPuntosAdminViewModel> ObtenerMovimientosDemo()
        {
            return
            [
                new() { Usuario = "Maria Montero Cruz", Tipo = "Ganados", Puntos = 50, Motivo = "Compra ORD-1042", Fecha = "02/08/2026" },
        new() { Usuario = "Carla Ramírez Solís", Tipo = "Canjeados", Puntos = -500, Motivo = "Descuento 10% en catálogo", Fecha = "01/08/2026" },
        new() { Usuario = "Luis Fernández Rojas", Tipo = "Ajuste", Puntos = 200, Motivo = "Compensación por soporte", Fecha = "30/07/2026" }
            ];
        }
        [HttpGet]
        public IActionResult AsistenciaQr()
        {
            return View(ObtenerActividadesQrDemo());
        }

        [HttpGet]
        public IActionResult DetalleAsistenciaQr(int id = 1)
        {
            var actividad = ObtenerActividadesQrDemo().FirstOrDefault(a => a.Id == id) ?? ObtenerActividadesQrDemo().First();
            return View(actividad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrAsistencia(int id)
        {
            TempData["MensajeAdmin"] = "El código QR de asistencia fue generado correctamente.";
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegenerarQrAsistencia(int id)
        {
            TempData["MensajeAdmin"] = "El código QR fue regenerado. El anterior ya no es válido.";
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpGet]
        public IActionResult DescargarQrAsistencia(int id)
        {
            // Demo: se sirve una imagen de ejemplo. Reemplazar por la generación real del QR.
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ValidarAsistencia(int id, string correo)
        {
            TempData["MensajeAdmin"] = $"Se registró la asistencia de {correo}.";
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        private static List<ActividadQrAdminViewModel> ObtenerActividadesQrDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte Inarrivo San Pedro", Tipo = "Evento", Fecha = "15 y 16 de agosto", QrGenerado = true,
            Asistentes = [ new() { Nombre = "Maria Montero Cruz", Correo = "maria@ejemplo.com", Presente = true },
                           new() { Nombre = "Carla Ramírez Solís", Correo = "carla@ejemplo.com", Presente = false } ] },
        new() { Id = 2, Nombre = "Cerámica para principiantes", Tipo = "Taller", Fecha = "23 de agosto", QrGenerado = true,
            Asistentes = [ new() { Nombre = "Luis Fernández Rojas", Correo = "luis@ejemplo.com", Presente = false } ] },
        new() { Id = 3, Nombre = "Feria Creativa Santa Ana", Tipo = "Evento", Fecha = "5 al 7 de septiembre", QrGenerado = false, Asistentes = [] }
            ];
        }

        // ---------- NOTIFICACIONES ----------
        [HttpGet]
        public IActionResult Notificaciones(string pestana = "Enviar")
        {
            return View(new NotificacionesAdminViewModel
            {
                Pestana = pestana,
                Historial = ObtenerNotificacionesDemo()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnviarNotificacion(NotificacionFormViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Notificaciones), new NotificacionesAdminViewModel
                {
                    Pestana = "Enviar",
                    Nueva = modelo,
                    Historial = ObtenerNotificacionesDemo()
                });
            }

            string destinatario = modelo.TipoDestinatario switch
            {
                "Individual" => $"Usuario: {modelo.UsuarioDestino}",
                "Grupo" => $"Grupo: {modelo.GrupoDestino}",
                _ => "Todos los usuarios"
            };

            TempData["MensajeAdmin"] = $"La notificación \"{modelo.Titulo}\" fue enviada a {destinatario}.";
            return RedirectToAction(nameof(Notificaciones), new { pestana = "Historial" });
        }

        private static List<NotificacionEnviadaViewModel> ObtenerNotificacionesDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Nueva feria en San Pedro", Mensaje = "Inscribite antes del 10 de agosto.", Destinatario = "Todos los usuarios", Fecha = "04/08/2026", TotalDestinatarios = 486 },
                new() { Id = 2, Titulo = "Recordatorio de suscripción", Mensaje = "Tu plan vence pronto, renová para seguir disfrutando de tus beneficios.", Destinatario = "Grupo: Emprendedores", Fecha = "02/08/2026", TotalDestinatarios = 57 },
                new() { Id = 3, Titulo = "Bienvenida al club", Mensaje = "Gracias por unirte a Club Creativo.", Destinatario = "Usuario: maria@ejemplo.com", Fecha = "30/07/2026", TotalDestinatarios = 1 }
            ];
        }

        // ---------- GALERÍAS ----------
        [HttpGet]
        public IActionResult Galerias()
        {
            return View(ObtenerGaleriasDemo());
        }

        [HttpGet]
        public IActionResult CrearGaleria()
        {
            ViewData["Titulo"] = "Crear galería";
            return View("FormularioGaleria", new GaleriaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearGaleria(GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear galería";
            if (!ModelState.IsValid) return View("FormularioGaleria", modelo);
            TempData["MensajeAdmin"] = $"La galería \"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        [HttpGet]
        public IActionResult EditarGaleria(int id = 1)
        {
            ViewData["Titulo"] = "Editar galería";
            var galeria = ObtenerGaleriasDemo().FirstOrDefault(g => g.Id == id) ?? new GaleriaAdminViewModel { Id = id };
            return View("FormularioGaleria", galeria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarGaleria(GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar galería";
            if (!ModelState.IsValid) return View("FormularioGaleria", modelo);
            TempData["MensajeAdmin"] = $"La galería \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargarImagenesGaleria(int id, List<IFormFile> imagenes)
        {
            if (imagenes is { Count: > 0 })
            {
                string carpeta = Path.Combine(_entornoWeb.WebRootPath, "uploads", "galerias");
                Directory.CreateDirectory(carpeta);

                foreach (var archivo in imagenes)
                {
                    if (archivo.Length <= 0) continue;
                    string nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                    string rutaCompleta = Path.Combine(carpeta, nombreArchivo);
                    using var flujo = new FileStream(rutaCompleta, FileMode.Create);
                    await archivo.CopyToAsync(flujo);
                }

                TempData["MensajeAdmin"] = $"Se cargaron {imagenes.Count} imagen(es) a la galería correctamente.";
            }
            else
            {
                TempData["MensajeAdmin"] = "Seleccioná al menos una imagen para cargar.";
            }

            return RedirectToAction(nameof(EditarGaleria), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarImagenGaleria(int id, int imagenId)
        {
            TempData["MensajeAdmin"] = "La imagen fue eliminada de la galería.";
            return RedirectToAction(nameof(EditarGaleria), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarGaleria(int id)
        {
            TempData["MensajeAdmin"] = "La galería fue eliminada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        private static List<GaleriaAdminViewModel> ObtenerGaleriasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte Inarrivo San Pedro 2026", Descripcion = "Fotografías del evento realizado en San Pedro.", Categoria = "Eventos", Estado = "Publicada", Portada = "/images/logo.jpg", FechaCreacion = "16/08/2026",
                    Imagenes = [ new() { Id = 1, Url = "/images/logo.jpg", NombreArchivo = "portada.jpg" }, new() { Id = 2, Url = "/images/logo.jpg", NombreArchivo = "stand-1.jpg" } ] },
                new() { Id = 2, Nombre = "Taller de cerámica", Descripcion = "Registro fotográfico del taller de cerámica para principiantes.", Categoria = "Talleres", Estado = "Publicada", Portada = "/images/logo.jpg", FechaCreacion = "24/08/2026", Imagenes = [] },
                new() { Id = 3, Nombre = "Emprendimientos destacados", Descripcion = "Galería con productos de emprendimientos aliados.", Categoria = "Emprendimientos", Estado = "Oculta", Portada = "/images/logo.jpg", FechaCreacion = "10/07/2026", Imagenes = [] }
            ];
        }

        // ---------- NOTICIAS Y ANUNCIOS ----------
        [HttpGet]
        public IActionResult Noticias(string estado = "Todas")
        {
            var noticias = ObtenerNoticiasDemo();
            return View(new ListadoNoticiasAdminViewModel
            {
                FiltroEstado = estado,
                Noticias = estado == "Todas" ? noticias : noticias.Where(n => n.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearNoticia()
        {
            ViewData["Titulo"] = "Crear publicación";
            return View("FormularioNoticia", new NoticiaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearNoticia(NoticiaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear publicación";
            if (!ModelState.IsValid) return View("FormularioNoticia", modelo);
            TempData["MensajeAdmin"] = $"La publicación \"{modelo.Titulo}\" fue creada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpGet]
        public IActionResult EditarNoticia(int id = 1)
        {
            ViewData["Titulo"] = "Editar publicación";
            var noticia = ObtenerNoticiasDemo().FirstOrDefault(n => n.Id == id) ?? new NoticiaAdminViewModel { Id = id };
            return View("FormularioNoticia", noticia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarNoticia(NoticiaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar publicación";
            if (!ModelState.IsValid) return View("FormularioNoticia", modelo);
            TempData["MensajeAdmin"] = $"La publicación \"{modelo.Titulo}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublicarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue publicada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue desactivada.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue eliminada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        private static List<NoticiaAdminViewModel> ObtenerNoticiasDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Nueva alianza con artesanos locales", Contenido = "Club Creativo firma una alianza para fortalecer el catálogo de productos artesanales.", Categoria = "Noticia", Estado = "Publicada", Autor = "Jossete Sánchez", FechaPublicacion = "01/08/2026" },
                new() { Id = 2, Titulo = "Cambios en el horario de atención", Contenido = "A partir de setiembre el horario de atención se extiende hasta las 7:00 p. m.", Categoria = "Anuncio", Estado = "Publicada", Autor = "Jossete Sánchez", FechaPublicacion = "28/07/2026" },
                new() { Id = 3, Titulo = "Próxima feria en Heredia", Contenido = "Se viene una nueva edición de la Feria Creativa en Heredia este 20 de setiembre.", Categoria = "Anuncio", Estado = "Borrador", Autor = "Jossete Sánchez", FechaPublicacion = "" },
                new() { Id = 4, Titulo = "Resultados de la encuesta 2025", Contenido = "Compartimos los resultados de la última encuesta de satisfacción de los usuarios.", Categoria = "Noticia", Estado = "Inactiva", Autor = "Jossete Sánchez", FechaPublicacion = "15/03/2026" }
            ];
        }

        // ---------- REPORTES Y ESTADÍSTICAS ----------
        [HttpGet]
        public IActionResult Reportes(FiltroReporteViewModel filtro)
        {
            filtro ??= new FiltroReporteViewModel();
            return View(ConstruirReporte(filtro));
        }

        [HttpGet]
        public IActionResult ExportarReporte(FiltroReporteViewModel filtro)
        {
            ReporteAdminViewModel modelo = ConstruirReporte(filtro ?? new FiltroReporteViewModel());

            var constructor = new System.Text.StringBuilder();
            constructor.AppendLine(string.Join(",", modelo.Columnas));
            foreach (var fila in modelo.Filas)
            {
                constructor.AppendLine(string.Join(",", fila.Columna1, fila.Columna2, fila.Columna3, fila.Columna4));
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(constructor.ToString());
            string nombreArchivo = $"reporte-{modelo.Filtro.TipoReporte.ToLower()}.csv";
            return File(bytes, "text/csv", nombreArchivo);
        }

        private static ReporteAdminViewModel ConstruirReporte(FiltroReporteViewModel filtro)
        {
            (List<PuntoGraficoViewModel> barras, List<PuntoGraficoViewModel> dona, string[] columnas, List<FilaReporteViewModel> filas, string titulo) = filtro.TipoReporte switch
            {
                "Usuarios" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 40 }, new() { Etiqueta = "Abr", Valor = 55 }, new() { Etiqueta = "May", Valor = 48 }, new() { Etiqueta = "Jun", Valor = 63 }, new() { Etiqueta = "Jul", Valor = 70 }, new() { Etiqueta = "Ago", Valor = 82 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Usuario", Valor = 340 }, new() { Etiqueta = "Emprendedor", Valor = 120 }, new() { Etiqueta = "Administrador", Valor = 6 } },
                    new[] { "Usuario", "Rol", "Registro", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Maria Montero Cruz", Columna2 = "Usuario", Columna3 = "12/01/2026", Columna4 = "Activo" },
                        new() { Columna1 = "Valentín Arce Mora", Columna2 = "Emprendedor", Columna3 = "20/05/2026", Columna4 = "Activo" }
                    },
                    "Reporte de usuarios"
                ),
                "Emprendimientos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 4 }, new() { Etiqueta = "Abr", Valor = 6 }, new() { Etiqueta = "May", Valor = 5 }, new() { Etiqueta = "Jun", Valor = 9 }, new() { Etiqueta = "Jul", Valor = 8 }, new() { Etiqueta = "Ago", Valor = 12 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Aprobados", Valor = 42 }, new() { Etiqueta = "Pendientes", Valor = 8 }, new() { Etiqueta = "Rechazados", Valor = 7 } },
                    new[] { "Emprendimiento", "Categoría", "Solicitud", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Artesanías MiVo", Columna2 = "Arte e ilustración", Columna3 = "01/08/2026", Columna4 = "Pendiente" },
                        new() { Columna1 = "Orquídea", Columna2 = "Accesorios", Columna3 = "10/07/2026", Columna4 = "Aprobado" }
                    },
                    "Reporte de emprendimientos"
                ),
                "Productos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 30 }, new() { Etiqueta = "Abr", Valor = 42 }, new() { Etiqueta = "May", Valor = 38 }, new() { Etiqueta = "Jun", Valor = 55 }, new() { Etiqueta = "Jul", Valor = 60 }, new() { Etiqueta = "Ago", Valor = 72 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Arte e ilustración", Valor = 34 }, new() { Etiqueta = "Accesorios", Valor = 26 }, new() { Etiqueta = "Hogar y decoración", Valor = 20 } },
                    new[] { "Producto", "Emprendimiento", "Categoría", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Aretes Orquídea", Columna2 = "Orquídea", Columna3 = "Accesorios", Columna4 = "Activo" },
                        new() { Columna1 = "Vela Aromática Lavanda", Columna2 = "Luz Natural", Columna3 = "Velas y aromas", Columna4 = "Activo" }
                    },
                    "Reporte de productos"
                ),
                "Eventos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 1 }, new() { Etiqueta = "Abr", Valor = 2 }, new() { Etiqueta = "May", Valor = 1 }, new() { Etiqueta = "Jun", Valor = 3 }, new() { Etiqueta = "Jul", Valor = 2 }, new() { Etiqueta = "Ago", Valor = 3 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Programados", Valor = 2 }, new() { Etiqueta = "Finalizados", Valor = 6 }, new() { Etiqueta = "Cancelados", Valor = 1 } },
                    new[] { "Evento", "Ubicación", "Fecha", "Inscritos" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Arte Inarrivo San Pedro", Columna2 = "San Pedro", Columna3 = "15 y 16 de agosto", Columna4 = "120" },
                        new() { Columna1 = "Feria Creativa Santa Ana", Columna2 = "Santa Ana", Columna3 = "5 al 7 de setiembre", Columna4 = "86" }
                    },
                    "Reporte de eventos"
                ),
                _ => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 45 }, new() { Etiqueta = "Abr", Valor = 58 }, new() { Etiqueta = "May", Valor = 50 }, new() { Etiqueta = "Jun", Valor = 72 }, new() { Etiqueta = "Jul", Valor = 65 }, new() { Etiqueta = "Ago", Valor = 90 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Pagadas", Valor = 58 }, new() { Etiqueta = "Pendientes", Valor = 12 }, new() { Etiqueta = "Canceladas", Valor = 6 } },
                    new[] { "Orden", "Cliente", "Fecha", "Total" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "ORD-1042", Columna2 = "Maria Montero Cruz", Columna3 = "02/08/2026", Columna4 = "₡24 000" },
                        new() { Columna1 = "ORD-1043", Columna2 = "Carla Ramírez Solís", Columna3 = "03/08/2026", Columna4 = "₡13 000" }
                    },
                    "Reporte de ventas"
                )
            };

            return new ReporteAdminViewModel
            {
                Filtro = filtro,
                GraficoBarras = barras,
                GraficoDona = dona,
                Columnas = columnas.ToList(),
                Filas = filas,
                Titulo = titulo
            };
        }

        // ---------- MODERACIÓN DE COMENTARIOS ----------
        [HttpGet]
        public IActionResult Comentarios(string? busqueda, string estado = "Todos", string origen = "Todos")
        {
            var comentarios = ObtenerComentariosDemo();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                comentarios = comentarios
                    .Where(c => c.Contenido.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                                c.Autor.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (estado != "Todos") comentarios = comentarios.Where(c => c.Estado == estado).ToList();
            if (origen != "Todos") comentarios = comentarios.Where(c => c.Origen == origen).ToList();

            return View(new ListadoComentariosAdminViewModel
            {
                Busqueda = busqueda,
                FiltroEstado = estado,
                FiltroOrigen = origen,
                Comentarios = comentarios
            });
        }

        [HttpGet]
        public IActionResult VerComentario(int id = 1)
        {
            var comentario = ObtenerComentariosDemo().FirstOrDefault(c => c.Id == id) ?? ObtenerComentariosDemo().First();
            return View(comentario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarComentario(int id)
        {
            TempData["MensajeAdmin"] = "El comentario fue eliminado por contenido inapropiado.";
            return RedirectToAction(nameof(Comentarios));
        }

        private static List<ComentarioAdminViewModel> ObtenerComentariosDemo()
        {
            return
            [
                new() { Id = 1, Autor = "Maria Montero Cruz", Contenido = "¡Excelente producto, llegó muy rápido y en perfecto estado!", Origen = "Producto", ElementoRelacionado = "Aretes Orquídea", Fecha = "03/08/2026", Estado = "Visible", Reportes = 0 },
                new() { Id = 2, Autor = "Usuario anónimo", Contenido = "Contenido inapropiado reportado por varios usuarios de la comunidad.", Origen = "Noticia", ElementoRelacionado = "Cambios en el horario de atención", Fecha = "02/08/2026", Estado = "Reportado", Reportes = 4 },
                new() { Id = 3, Autor = "Luis Fernández Rojas", Contenido = "Muy buen taller, aprendí bastante sobre cerámica.", Origen = "Taller", ElementoRelacionado = "Cerámica para principiantes", Fecha = "24/08/2026", Estado = "Visible", Reportes = 0 },
                new() { Id = 4, Autor = "Carla Ramírez Solís", Contenido = "El evento estuvo muy desorganizado, no lo recomiendo para nada.", Origen = "Evento", ElementoRelacionado = "Feria Creativa Santa Ana", Fecha = "07/09/2026", Estado = "Reportado", Reportes = 2 }
            ];
        }

        // ---------- CATEGORÍAS Y ETIQUETAS ----------
        [HttpGet]
        public IActionResult Categorias(string tipo = "Categoria")
        {
            var categorias = ObtenerCategoriasDemo();
            return View(new ListadoCategoriasAdminViewModel
            {
                FiltroTipo = tipo,
                Categorias = categorias.Where(c => c.Tipo == tipo).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearCategoria(string tipo = "Categoria")
        {
            ViewData["Titulo"] = tipo == "Etiqueta" ? "Crear etiqueta" : "Crear categoría";
            return View("FormularioCategoria", new CategoriaAdminViewModel { Tipo = tipo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta" ? "Crear etiqueta" : "Crear categoría";
            if (!ModelState.IsValid) return View("FormularioCategoria", modelo);
            TempData["MensajeAdmin"] = $"\"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo = modelo.Tipo });
        }

        [HttpGet]
        public IActionResult EditarCategoria(int id = 1)
        {
            var categoria = ObtenerCategoriasDemo().FirstOrDefault(c => c.Id == id) ?? new CategoriaAdminViewModel { Id = id };
            ViewData["Titulo"] = categoria.Tipo == "Etiqueta" ? "Editar etiqueta" : "Editar categoría";
            return View("FormularioCategoria", categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta" ? "Editar etiqueta" : "Editar categoría";
            if (!ModelState.IsValid) return View("FormularioCategoria", modelo);
            TempData["MensajeAdmin"] = $"\"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo = modelo.Tipo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarCategoria(int id, string tipo = "Categoria")
        {
            TempData["MensajeAdmin"] = "El elemento fue eliminado correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo });
        }

        [HttpGet]
        public IActionResult ElementosCategoria(int id = 1)
        {
            var categoria = ObtenerCategoriasDemo().FirstOrDefault(c => c.Id == id) ?? ObtenerCategoriasDemo().First();
            return View(new ElementosCategoriaViewModel
            {
                Categoria = categoria,
                Elementos = ObtenerElementosAsociadosDemo(categoria.Modulo)
            });
        }

        private static List<CategoriaAdminViewModel> ObtenerCategoriasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte e ilustración", Descripcion = "Productos relacionados con arte e ilustraciones.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 34 },
                new() { Id = 2, Nombre = "Accesorios", Descripcion = "Aretes, collares y accesorios artesanales.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 26 },
                new() { Id = 3, Nombre = "Hogar y decoración", Descripcion = "Artículos decorativos hechos a mano.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 20 },
                new() { Id = 4, Nombre = "Hecho a mano", Descripcion = "Etiqueta para productos artesanales.", Tipo = "Etiqueta", Modulo = "Productos", ElementosAsociados = 58 },
                new() { Id = 5, Nombre = "Nuevo", Descripcion = "Etiqueta para publicaciones recientes.", Tipo = "Etiqueta", Modulo = "Noticias", ElementosAsociados = 12 }
            ];
        }

        private static List<ElementoAsociadoViewModel> ObtenerElementosAsociadosDemo(string modulo)
        {
            return modulo switch
            {
                "Noticias" =>
                [
                    new() { Nombre = "Nueva alianza con artesanos locales", Tipo = "Noticia", Estado = "Publicada" },
                    new() { Nombre = "Próxima feria en Heredia", Tipo = "Anuncio", Estado = "Borrador" }
                ],
                _ =>
                [
                    new() { Nombre = "Aretes Orquídea", Tipo = "Producto", Estado = "Activo" },
                    new() { Nombre = "Vela Aromática Lavanda", Tipo = "Producto", Estado = "Activo" },
                    new() { Nombre = "Cuadro decorativo Boho", Tipo = "Producto", Estado = "Pausado" }
                ]
            };
        }

        // ---------- PROMOCIONES Y CAMPAÑAS ----------
        [HttpGet]
        public IActionResult Promociones(string estado = "Todas")
        {
            var promociones = ObtenerPromocionesDemo();
            return View(new ListadoPromocionesAdminViewModel
            {
                FiltroEstado = estado,
                Promociones = estado == "Todas" ? promociones : promociones.Where(p => p.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearPromocion()
        {
            ViewData["Titulo"] = "Crear promoción";
            return View("FormularioPromocion", new PromocionAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear promoción";
            if (!ModelState.IsValid) return View("FormularioPromocion", modelo);
            TempData["MensajeAdmin"] = $"La promoción \"{modelo.Nombre}\" fue programada del {modelo.FechaInicio:dd/MM/yyyy} al {modelo.FechaFin:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public IActionResult EditarPromocion(int id = 1)
        {
            ViewData["Titulo"] = "Editar promoción";
            var promocion = ObtenerPromocionesDemo().FirstOrDefault(p => p.Id == id) ?? new PromocionAdminViewModel { Id = id };
            return View("FormularioPromocion", promocion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar promoción";
            if (!ModelState.IsValid) return View("FormularioPromocion", modelo);
            TempData["MensajeAdmin"] = $"La promoción \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public IActionResult DetallePromocion(int id = 1)
        {
            var promocion = ObtenerPromocionesDemo().FirstOrDefault(p => p.Id == id) ?? ObtenerPromocionesDemo().First();
            return View(promocion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarPromocion(int id)
        {
            TempData["MensajeAdmin"] = "La promoción fue desactivada.";
            return RedirectToAction(nameof(Promociones));
        }

        private static List<PromocionAdminViewModel> ObtenerPromocionesDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Descuento Feria San Pedro", Descripcion = "15% de descuento en productos participantes de la feria.", TipoDescuento = "Porcentaje", ValorDescuento = 15, FechaInicio = new DateTime(2026, 8, 10), FechaFin = new DateTime(2026, 8, 16), Estado = "Activa", Emprendimiento = "Todos", Usos = 48 },
                new() { Id = 2, Nombre = "Envío gratis primera compra", Descripcion = "Envío sin costo para nuevos usuarios del club.", TipoDescuento = "Monto fijo", ValorDescuento = 2500, FechaInicio = new DateTime(2026, 9, 1), FechaFin = new DateTime(2026, 9, 30), Estado = "Programada", Emprendimiento = "Todos", Usos = 0 },
                new() { Id = 3, Nombre = "Aniversario Orquídea", Descripcion = "20% de descuento en toda la tienda Orquídea.", TipoDescuento = "Porcentaje", ValorDescuento = 20, FechaInicio = new DateTime(2026, 6, 1), FechaFin = new DateTime(2026, 6, 15), Estado = "Finalizada", Emprendimiento = "Orquídea", Usos = 76 },
                new() { Id = 4, Nombre = "Descuento talleres agosto", Descripcion = "10% de descuento en inscripciones a talleres.", TipoDescuento = "Porcentaje", ValorDescuento = 10, FechaInicio = new DateTime(2026, 8, 1), FechaFin = new DateTime(2026, 8, 31), Estado = "Desactivada", Emprendimiento = "Todos", Usos = 5 }
            ];
        }

        // ---------- ENCUESTAS ----------
        [HttpGet]
        public IActionResult Encuestas(string estado = "Todas")
        {
            var encuestas = ObtenerEncuestasDemo();
            return View(new ListadoEncuestasAdminViewModel
            {
                FiltroEstado = estado,
                Encuestas = estado == "Todas" ? encuestas : encuestas.Where(e => e.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearEncuesta()
        {
            ViewData["Titulo"] = "Crear encuesta";
            return View("FormularioEncuesta", new EncuestaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearEncuesta(EncuestaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear encuesta";
            if (!ModelState.IsValid) return View("FormularioEncuesta", modelo);
            int totalPreguntas = modelo.PreguntasTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
            TempData["MensajeAdmin"] = $"La encuesta \"{modelo.Titulo}\" fue creada con {totalPreguntas} pregunta(s).";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public IActionResult EditarEncuesta(int id = 1)
        {
            ViewData["Titulo"] = "Editar encuesta";
            var encuesta = ObtenerEncuestasDemo().FirstOrDefault(e => e.Id == id) ?? new EncuestaAdminViewModel { Id = id };
            return View("FormularioEncuesta", encuesta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarEncuesta(EncuestaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar encuesta";
            if (!ModelState.IsValid) return View("FormularioEncuesta", modelo);
            TempData["MensajeAdmin"] = $"La encuesta \"{modelo.Titulo}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublicarEncuesta(int id)
        {
            TempData["MensajeAdmin"] = "La encuesta fue publicada correctamente.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarEncuesta(int id)
        {
            TempData["MensajeAdmin"] = "La encuesta fue cerrada. Ya no se aceptan más respuestas.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public IActionResult ResultadosEncuesta(int id = 1)
        {
            var encuesta = ObtenerEncuestasDemo().FirstOrDefault(e => e.Id == id) ?? ObtenerEncuestasDemo().First();
            return View(encuesta);
        }

        private static List<EncuestaAdminViewModel> ObtenerEncuestasDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Satisfacción con la feria de agosto", Descripcion = "Ayudanos a mejorar futuras ediciones del evento.", Estado = "Publicada", FechaCreacion = "16/08/2026", TotalRespuestas = 128,
                    PreguntasTexto = "¿Cómo calificarías la organización del evento?\n¿Qué tan probable es que asistas a la próxima edición?",
                    Preguntas =
                    [
                        new() { Texto = "¿Cómo calificarías la organización del evento?", Opciones = [ new() { Texto = "Excelente", Votos = 62 }, new() { Texto = "Buena", Votos = 48 }, new() { Texto = "Regular", Votos = 14 }, new() { Texto = "Mala", Votos = 4 } ] },
                        new() { Texto = "¿Qué tan probable es que asistas a la próxima edición?", Opciones = [ new() { Texto = "Muy probable", Votos = 80 }, new() { Texto = "Probable", Votos = 35 }, new() { Texto = "Poco probable", Votos = 13 } ] }
                    ] },
                new() { Id = 2, Titulo = "Nuevas categorías de productos", Descripcion = "Consulta sobre categorías de interés para el catálogo.", Estado = "Borrador", FechaCreacion = "01/08/2026", TotalRespuestas = 0,
                    PreguntasTexto = "¿Qué categoría te gustaría ver en el catálogo?",
                    Preguntas = [ new() { Texto = "¿Qué categoría te gustaría ver en el catálogo?", Opciones = [ new() { Texto = "Ropa artesanal", Votos = 0 }, new() { Texto = "Juguetes de madera", Votos = 0 } ] } ] },
                new() { Id = 3, Titulo = "Evaluación del taller de cerámica", Descripcion = "Retroalimentación de los participantes del taller.", Estado = "Cerrada", FechaCreacion = "10/06/2026", TotalRespuestas = 45,
                    PreguntasTexto = "¿Recomendarías este taller a otras personas?",
                    Preguntas = [ new() { Texto = "¿Recomendarías este taller a otras personas?", Opciones = [ new() { Texto = "Sí", Votos = 41 }, new() { Texto = "No", Votos = 4 } ] } ] }
            ];
        }

        // ---------- SEGURIDAD Y AUDITORÍA ----------
        [HttpGet]
        public IActionResult Bitacora(FiltroBitacoraViewModel filtro)
        {
            filtro ??= new FiltroBitacoraViewModel();
            var movimientos = ObtenerMovimientosBitacoraDemo();

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                movimientos = movimientos.Where(m => m.Usuario.Contains(filtro.Usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filtro.Accion) && filtro.Accion != "Todas")
            {
                movimientos = movimientos.Where(m => m.Accion == filtro.Accion).ToList();
            }

            if (filtro.FechaInicio is not null)
            {
                movimientos = movimientos.Where(m =>
                    DateTime.TryParseExact(m.Fecha.Split(' ')[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha) &&
                    fecha >= filtro.FechaInicio).ToList();
            }

            if (filtro.FechaFin is not null)
            {
                movimientos = movimientos.Where(m =>
                    DateTime.TryParseExact(m.Fecha.Split(' ')[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha) &&
                    fecha <= filtro.FechaFin).ToList();
            }

            return View(new BitacoraAdminViewModel
            {
                Filtro = filtro,
                Movimientos = movimientos,
                Alertas = ObtenerAlertasSeguridadDemo(),
                IntentosFallidos = ObtenerIntentosFallidosDemo()
            });
        }

        [HttpGet]
        public IActionResult DetalleMovimiento(int id = 1)
        {
            var movimiento = ObtenerMovimientosBitacoraDemo().FirstOrDefault(m => m.Id == id) ?? ObtenerMovimientosBitacoraDemo().First();
            return View(movimiento);
        }

        private static List<MovimientoBitacoraViewModel> ObtenerMovimientosBitacoraDemo()
        {
            return
            [
                new() { Id = 1, Usuario = "Jossete Sánchez", Accion = "Inicio de sesión", Modulo = "Acceso", Fecha = "05/08/2026 08:15", DireccionIp = "190.113.20.4", Detalle = "Inicio de sesión exitoso desde el panel de administración.", Nivel = "informativa" },
                new() { Id = 2, Usuario = "Jossete Sánchez", Accion = "Aprobación", Modulo = "Emprendimientos", Fecha = "05/08/2026 09:02", DireccionIp = "190.113.20.4", Detalle = "Se aprobó la solicitud de emprendimiento \"Luz Natural\".", Nivel = "informativa" },
                new() { Id = 3, Usuario = "Carla Ramírez Solís", Accion = "Eliminación", Modulo = "Comentarios", Fecha = "05/08/2026 10:40", DireccionIp = "201.203.14.88", Detalle = "Se eliminó un comentario reportado por contenido inapropiado.", Nivel = "advertencia" },
                new() { Id = 4, Usuario = "Desconocido", Accion = "Intento fallido", Modulo = "Acceso", Fecha = "04/08/2026 22:18", DireccionIp = "45.66.12.9", Detalle = "Múltiples intentos fallidos de inicio de sesión para la cuenta admin@clubcreativomivo.com.", Nivel = "critica" },
                new() { Id = 5, Usuario = "Jossete Sánchez", Accion = "Edición", Modulo = "Promociones", Fecha = "04/08/2026 15:22", DireccionIp = "190.113.20.4", Detalle = "Se editaron las fechas de la promoción \"Descuento Feria San Pedro\".", Nivel = "informativa" }
            ];
        }

        private static List<AlertaAdminViewModel> ObtenerAlertasSeguridadDemo()
        {
            return
            [
                new() { Tipo = "Seguridad", Mensaje = "Se detectaron 5 intentos fallidos de inicio de sesión desde una misma dirección IP.", Fecha = "Hoy", Icono = "bi-shield-exclamation", Nivel = "critica" },
                new() { Tipo = "Cuentas", Mensaje = "La cuenta admin@clubcreativomivo.com fue bloqueada temporalmente.", Fecha = "Hoy", Icono = "bi-lock-fill", Nivel = "advertencia" }
            ];
        }

        private static List<IntentoFallidoViewModel> ObtenerIntentosFallidosDemo()
        {
            return
            [
                new() { Correo = "admin@clubcreativomivo.com", DireccionIp = "45.66.12.9", Fecha = "04/08/2026 22:18", Intentos = 5, Bloqueado = true },
                new() { Correo = "carla@ejemplo.com", DireccionIp = "201.203.14.88", Fecha = "03/08/2026 19:40", Intentos = 2, Bloqueado = false }
            ];
        }
    }
}
