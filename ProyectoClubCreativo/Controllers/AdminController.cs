using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels.Admin;

namespace ProyectoClubCreativo.Controllers
{
    public class AdminController : Controller
    {

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


    }
}
