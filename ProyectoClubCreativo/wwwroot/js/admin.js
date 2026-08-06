"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const botonAbrir = document.getElementById("abrirMenuAdmin");
    const botonCerrar = document.getElementById("cerrarMenuAdmin");
    const menu = document.getElementById("menuAdmin");
    const fondo = document.getElementById("fondoMenuAdmin");

    const abrirMenu = () => {
        if (!menu || !fondo) return;
        menu.classList.add("abierto");
        fondo.classList.add("visible");
        document.body.style.overflow = "hidden";
    };

    const cerrarMenu = () => {
        if (!menu || !fondo) return;
        menu.classList.remove("abierto");
        fondo.classList.remove("visible");
        document.body.style.overflow = "";
    };

    botonAbrir?.addEventListener("click", abrirMenu);
    botonCerrar?.addEventListener("click", cerrarMenu);
    fondo?.addEventListener("click", cerrarMenu);

    document.addEventListener("keydown", (evento) => {
        if (evento.key === "Escape") cerrarMenu();
    });

    // Alternar contraseña visible en el login de admin
    document.querySelectorAll("[data-toggle-password]").forEach((boton) => {
        boton.addEventListener("click", () => {
            const id = boton.getAttribute("data-toggle-password");
            const campo = document.getElementById(id);
            if (!campo) return;
            campo.type = campo.type === "password" ? "text" : "password";
        });
    });
});
