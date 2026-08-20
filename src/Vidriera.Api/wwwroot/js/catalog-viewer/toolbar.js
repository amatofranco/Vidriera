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

    if (dom.shareBtn) {
        if (!navigator.share || window.innerWidth > MOBILE_BREAKPOINT) {
            dom.shareBtn.style.display = "none";
        } else {
            dom.shareBtn.addEventListener("click", async () => {
                const companyName = document.querySelector(".cover-info-company")?.textContent || "Catálogo";
                try {
                    await navigator.share({ title: companyName, text: `Mirá el catálogo de ${companyName}`, url: location.href });
                } catch (e) {
                }
            });
        }
    }

    document.addEventListener("keydown", (e) => {
        if (e.key === "ArrowLeft") dom.prevBtn.click();
        else if (e.key === "ArrowRight") dom.nextBtn.click();
    });
}
