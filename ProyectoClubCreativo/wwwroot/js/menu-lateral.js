"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const botonAbrir = document.getElementById("botonAbrirMenu");
    const botonCerrar = document.getElementById("botonCerrarMenu");
    const menuLateral = document.getElementById("menuLateral");
    const fondoMenu = document.getElementById("fondoMenuLateral");

    if (!botonAbrir || !botonCerrar || !menuLateral || !fondoMenu) {
        console.warn("No se encontraron todos los elementos del menú lateral.");
        return;
    }

    const abrirMenu = () => {
        menuLateral.classList.add("abierto");
        fondoMenu.classList.add("visible");
        document.body.classList.add("menu-abierto");
        botonAbrir.setAttribute("aria-expanded", "true");
        menuLateral.setAttribute("aria-hidden", "false");
    };

    const cerrarMenu = () => {
        menuLateral.classList.remove("abierto");
        fondoMenu.classList.remove("visible");
        document.body.classList.remove("menu-abierto");
        botonAbrir.setAttribute("aria-expanded", "false");
        menuLateral.setAttribute("aria-hidden", "true");
    };

    botonAbrir.addEventListener("click", abrirMenu);
    botonCerrar.addEventListener("click", cerrarMenu);
    fondoMenu.addEventListener("click", cerrarMenu);

    document.addEventListener("keydown", (evento) => {
        if (evento.key === "Escape") {
            cerrarMenu();
        }
    });

    const enlacesMenu = menuLateral.querySelectorAll("a");

    enlacesMenu.forEach((enlace) => {
        enlace.addEventListener("click", cerrarMenu);
    });
});