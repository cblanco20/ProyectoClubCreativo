using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models.ViewModels;

namespace ProyectoClubCreativo.Controllers
{
    public class CuentaController : Controller
    {
        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View(new InicioSesionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IniciarSesion(InicioSesionViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeExito"] =
                "El formulario de inicio de sesión se validó correctamente.";

            return RedirectToAction(nameof(IniciarSesion));
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
        public IActionResult RegistroUsuario(RegistroUsuarioViewModel modelo)
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

            TempData["MensajePanel"] =
                $"¡Bienvenida, {modelo.Nombre}! Tu registro se validó correctamente.";

            return RedirectToAction(
                "Panel",
                "Usuario"
            );
        }

        [HttpGet]
        public IActionResult RegistroEmprendedor()
        {
            return View(new RegistroEmprendedorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistroEmprendedor(
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
                return View(modelo);
            }

            TempData["MensajeEmprendedor"] =
                $"¡Bienvenido, {modelo.Nombre}! Tu registro como emprendedor se validó correctamente.";

            return RedirectToAction(
                "Panel",
                "Emprendedor"
            );
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
    }
}
