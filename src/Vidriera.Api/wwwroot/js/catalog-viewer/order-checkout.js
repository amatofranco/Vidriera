import { renderDrawerItems, renderDrawerTotal, updateBadge } from "./order-cart-ui.js";

async function shareOrDownload(blob, fileName) {
    const file = new File([blob], fileName, {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });

    if (navigator.canShare && navigator.canShare({ files: [file] })) {
        try {
            await navigator.share({ files: [file], title: "Pedido" });
            return;
        } catch (e) {
            if (e && e.name === "AbortError") {
                // el usuario cerró el selector de compartir sin elegir nada — no es un error
                return;
            }
            // el share falló por otro motivo (activación de usuario perdida tras
            // los await, sin apps de destino, etc.) — se cae a la descarga
            // directa en vez de no hacer nada.
        }
    }

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
}

function parseFileName(response, fallback) {
    const disposition = response.headers.get("content-disposition") || "";
    const match = disposition.match(/filename="?([^";]+)"?/i);
    return match ? match[1] : fallback;
}

export function setupOrderUi(dom, cart, renderPageBar) {
    if (!dom.orderBadgeWrap) return;

    dom.orderBadgeWrap.addEventListener("click", () => {
        dom.orderDrawer.style.display = "flex";
    });

    if (dom.orderDrawerClose) {
        dom.orderDrawerClose.addEventListener("click", () => {
            dom.orderDrawer.style.display = "none";
        });
    }

    if (dom.orderCheckoutBtn) {
        dom.orderCheckoutBtn.addEventListener("click", () => {
            dom.orderDrawer.style.display = "none";
            dom.orderCheckoutModal.style.display = "flex";
        });
    }

    if (dom.orderModalClose) {
        dom.orderModalClose.addEventListener("click", () => {
            dom.orderCheckoutModal.style.display = "none";
        });
    }

    const orderSubmitBtnLabel = dom.orderSubmitBtn ? dom.orderSubmitBtn.textContent : "";

    if (dom.orderCheckoutForm) {
        dom.orderCheckoutForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            if (dom.orderFormError) dom.orderFormError.textContent = "";

            const form = dom.orderCheckoutForm;
            const customer = Object.fromEntries(new FormData(form));
            const items = cart.getItems().map((i) => ({ productId: i.productId, quantity: i.quantity }));
            const showPrices = cart.getTotalPrice() !== null;

            if (dom.orderSubmitBtn) {
                dom.orderSubmitBtn.disabled = true;
                dom.orderSubmitBtn.innerHTML = '<span class="order-btn-spinner"></span>Generando pedido...';
            }

            try {
                const response = await fetch("/api/orders/excel", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ companyId: dom.companyId, items, customer, showPrices }),
                });

                if (!response.ok) {
                    let message = `Error ${response.status}`;
                    try {
                        const body = await response.json();
                        message = body.detail || body.title || message;
                    } catch (parseError) {
                        // sin cuerpo JSON legible, se mantiene el mensaje genérico
                    }
                    throw new Error(message);
                }

                const fileName = parseFileName(response, "Pedido.xlsx");
                const blob = await response.blob();

                await shareOrDownload(blob, fileName);

                cart.clear();
                form.reset();
                dom.orderCheckoutModal.style.display = "none";
            } catch (err) {
                if (dom.orderFormError) {
                    dom.orderFormError.textContent = err && err.message ? err.message : "No se pudo generar el pedido.";
                }
            } finally {
                if (dom.orderSubmitBtn) {
                    dom.orderSubmitBtn.disabled = false;
                    dom.orderSubmitBtn.textContent = orderSubmitBtnLabel;
                }
            }
        });
    }

    cart.subscribe(() => {
        updateBadge(dom, cart);
        renderDrawerItems(dom, cart);
        renderDrawerTotal(dom, cart);
        renderPageBar();
    });

    updateBadge(dom, cart);
    renderDrawerItems(dom, cart);
    renderDrawerTotal(dom, cart);
}
