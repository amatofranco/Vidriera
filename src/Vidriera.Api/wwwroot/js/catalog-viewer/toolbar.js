import { MOBILE_BREAKPOINT } from "./layout.js";

export function setupToolbar(dom) {
    dom.fullscreenBtn.addEventListener("click", () => {
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            dom.stageEl.requestFullscreen().catch((e) => console.error("Fullscreen request failed:", e));
        }
    });

    dom.printBtn.addEventListener("click", () => {
        let printFrame = document.getElementById("print-frame");
        if (!printFrame) {
            printFrame = document.createElement("iframe");
            printFrame.id = "print-frame";
            printFrame.style.position = "fixed";
            printFrame.style.right = "0";
            printFrame.style.bottom = "0";
            printFrame.style.width = "0";
            printFrame.style.height = "0";
            printFrame.style.border = "0";
            document.body.appendChild(printFrame);
        }
        printFrame.onload = () => {
            printFrame.contentWindow.focus();
            printFrame.contentWindow.print();
        };
        printFrame.src = dom.fileUrl;
    });

    // navigator.share solo existe en navegadores que tienen selector nativo de
    // "compartir" — pero algunos navegadores desktop (ej. Edge en Windows)
    // también lo soportan, y ahí no queda bien mostrar el botón. Se oculta
    // salvo en mobile, en vez de armar un fallback (copiar al portapapeles,
    // etc.) que complica la UI.
    if (dom.shareBtn) {
        if (!navigator.share || window.innerWidth > MOBILE_BREAKPOINT) {
            dom.shareBtn.style.display = "none";
        } else {
            dom.shareBtn.addEventListener("click", async () => {
                const companyName = document.querySelector(".cover-info-company")?.textContent || "Catálogo";
                try {
                    await navigator.share({ title: companyName, text: `Mirá el catálogo de ${companyName}`, url: location.href });
                } catch (e) {
                    // el usuario cerró el selector de compartir sin elegir nada — no es un error
                }
            });
        }
    }

    document.addEventListener("keydown", (e) => {
        if (e.key === "ArrowLeft") dom.prevBtn.click();
        else if (e.key === "ArrowRight") dom.nextBtn.click();
    });
}
