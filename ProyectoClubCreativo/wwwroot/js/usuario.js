"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const botonAbrir = document.getElementById("abrirMenuUsuario");
    const botonCerrar = document.getElementById("cerrarMenuUsuario");
    const menu = document.getElementById("menuUsuario");
    const fondo = document.getElementById("fondoMenuUsuario");

    const abrirMenu = () => {
        if (!menu || !fondo) {
            return;
        }

        menu.classList.add("abierto");
        fondo.classList.add("visible");
        document.body.style.overflow = "hidden";
    };

    const cerrarMenu = () => {
        if (!menu || !fondo) {
            return;
        }

        menu.classList.remove("abierto");
        fondo.classList.remove("visible");
        document.body.style.overflow = "";
    };

    botonAbrir?.addEventListener("click", abrirMenu);
    botonCerrar?.addEventListener("click", cerrarMenu);
    fondo?.addEventListener("click", cerrarMenu);

    document.addEventListener("keydown", (evento) => {
        if (evento.key === "Escape") {
            cerrarMenu();
        }
    });

    const campoFotografia =
        document.getElementById("Fotografia");

    const vistaPrevia =
        document.getElementById("vistaPreviaPerfil");

    campoFotografia?.addEventListener("change", () => {
        const archivo = campoFotografia.files?.[0];

        if (!archivo || !vistaPrevia) {
            return;
        }

        if (!archivo.type.startsWith("image/")) {
            campoFotografia.value = "";
            return;
        }

        const lector = new FileReader();

        lector.onload = (evento) => {
            vistaPrevia.src = evento.target?.result;
        };

        lector.readAsDataURL(archivo);
    });
});