document.addEventListener("DOMContentLoaded", () => {

    const botonAbrir =
        document.getElementById("abrirMenuEmprendedor");

    const botonCerrar =
        document.getElementById("cerrarMenuEmprendedor");

    const menu =
        document.getElementById("menuEmprendedor");

    const fondo =
        document.getElementById("fondoMenuEmprendedor");

    const abrirMenu = () => {

        if (!menu || !fondo) {
            return;
        }

        menu.classList.add("abierto");
        fondo.classList.add("visible");

        document.body.classList.add(
            "menu-emprendedor-abierto"
        );

        botonAbrir?.setAttribute(
            "aria-expanded",
            "true"
        );
    };

    const cerrarMenu = () => {

        if (!menu || !fondo) {
            return;
        }

        menu.classList.remove("abierto");
        fondo.classList.remove("visible");

        document.body.classList.remove(
            "menu-emprendedor-abierto"
        );

        botonAbrir?.setAttribute(
            "aria-expanded",
            "false"
        );
    };

    botonAbrir?.addEventListener(
        "click",
        abrirMenu
    );

    botonCerrar?.addEventListener(
        "click",
        cerrarMenu
    );

    fondo?.addEventListener(
        "click",
        cerrarMenu
    );

    document.addEventListener(
        "keydown",
        (evento) => {

            if (evento.key === "Escape") {
                cerrarMenu();
            }
        }
    );

    window.addEventListener(
        "resize",
        () => {

            if (window.innerWidth > 1100) {
                cerrarMenu();
            }
        }
    );

    const enlacesMenu =
        document.querySelectorAll(
            ".navegacion-emprendedor a"
        );

    enlacesMenu.forEach((enlace) => {

        enlace.addEventListener(
            "click",
            () => {

                if (window.innerWidth <= 1100) {
                    cerrarMenu();
                }
            }
        );
    });
});