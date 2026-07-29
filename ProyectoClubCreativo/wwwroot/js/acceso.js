"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const botonesContrasena =
        document.querySelectorAll("[data-toggle-password]");

    botonesContrasena.forEach((boton) => {
        boton.addEventListener("click", () => {
            const identificador = boton.getAttribute("data-toggle-password");
            const campo = document.getElementById(identificador);

            if (!campo) {
                return;
            }

            const mostrar = campo.type === "password";
            campo.type = mostrar ? "text" : "password";
            boton.textContent = mostrar ? "◉" : "◉";

            boton.setAttribute(
                "aria-label",
                mostrar ? "Ocultar contraseña" : "Mostrar contraseña"
            );
        });
    });
});