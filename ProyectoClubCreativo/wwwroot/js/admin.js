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

    document.querySelectorAll("[data-toggle-password]").forEach((boton) => {
        boton.addEventListener("click", () => {
            const id = boton.getAttribute("data-toggle-password");
            const campo = document.getElementById(id);
            if (!campo) return;
            campo.type = campo.type === "password" ? "text" : "password";
        });
    });

    const mostrarAvisoExportacion = (texto, esError) => {
        const aviso = document.getElementById("avisoExportacion");
        if (!aviso) return;
        aviso.className = esError ? "alerta-error-admin" : "alerta-exito-admin";
        aviso.innerHTML = "";
        const icono = document.createElement("i");
        icono.className = esError ? "bi bi-exclamation-circle-fill" : "bi bi-check-circle-fill";
        aviso.appendChild(icono);
        aviso.appendChild(document.createTextNode(" " + texto));
        aviso.hidden = false;
    };

    document.querySelectorAll("[data-exportar]").forEach((enlace) => {
        enlace.addEventListener("click", async (evento) => {
            evento.preventDefault();
            try {
                const respuesta = await fetch(enlace.href, { headers: { "X-Requested-With": "fetch" } });
                if (!respuesta.ok) {
                    let mensaje = "No se pudo exportar el reporte.";
                    try {
                        const datos = await respuesta.json();
                        if (datos.mensaje) mensaje = datos.mensaje;
                    } catch { }
                    mostrarAvisoExportacion(mensaje, true);
                    return;
                }
                const disposicion = respuesta.headers.get("Content-Disposition") || "";
                const coincidencia = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposicion);
                const nombre = coincidencia ? decodeURIComponent(coincidencia[1]) : "reporte";
                const blob = await respuesta.blob();
                const url = URL.createObjectURL(blob);
                const descarga = document.createElement("a");
                descarga.href = url;
                descarga.download = nombre;
                document.body.appendChild(descarga);
                descarga.click();
                descarga.remove();
                URL.revokeObjectURL(url);
                mostrarAvisoExportacion("El reporte fue exportado correctamente.", false);
            } catch {
                mostrarAvisoExportacion("No se pudo exportar el reporte.", true);
            }
        });
    });
});
