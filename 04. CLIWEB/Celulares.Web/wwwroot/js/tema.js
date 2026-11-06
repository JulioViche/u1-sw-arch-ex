(() => {
    const boton = document.getElementById("theme-toggle");
    const aplicar = tema => {
        document.documentElement.dataset.theme = tema;
        const claro = tema === "claro";
        boton?.setAttribute("aria-pressed", String(claro));
        boton?.setAttribute("aria-label", claro ? "Cambiar a tema oscuro" : "Cambiar a tema claro");
        if (boton) boton.textContent = claro ? "Tema oscuro" : "Tema claro";
        document.querySelector('meta[name="theme-color"]')?.setAttribute("content", claro ? "#f3f6fb" : "#080d18");
    };
    aplicar(document.documentElement.dataset.theme === "claro" ? "claro" : "oscuro");
    boton?.addEventListener("click", () => {
        const siguiente = document.documentElement.dataset.theme === "claro" ? "oscuro" : "claro";
        try { localStorage.setItem("celulares-tema", siguiente); } catch { }
        aplicar(siguiente);
    });
})();
