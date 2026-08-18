const STORAGE_PREFIX = "vidriera-order-";

function loadStoredItems(storageKey) {
    if (!storageKey) return new Map();
    try {
        const raw = sessionStorage.getItem(storageKey);
        if (!raw) return new Map();
        return new Map(Object.entries(JSON.parse(raw)));
    } catch (e) {
        return new Map();
    }
}

function createCartState(dom) {
    const storageKey = dom.companyId ? STORAGE_PREFIX + dom.companyId : null;
    const items = loadStoredItems(storageKey);
    const listeners = [];

    function persist() {
        if (!storageKey) return;
        sessionStorage.setItem(storageKey, JSON.stringify(Object.fromEntries(items)));
    }

    function notify() {
        persist();
        listeners.forEach((fn) => fn());
    }

    return {
        subscribe(fn) {
            listeners.push(fn);
        },
        getItems() {
            return Array.from(items.entries()).map(([productId, entry]) => ({
                productId,
                name: entry.name,
                quantity: entry.quantity,
            }));
        },
        getQuantity(productId) {
            return items.get(productId)?.quantity || 0;
        },
        getTotalQuantity() {
            let total = 0;
            for (const entry of items.values()) total += entry.quantity;
            return total;
        },
        setQuantity(productId, name, quantity) {
            if (quantity <= 0) {
                items.delete(productId);
            } else {
                items.set(productId, { name, quantity });
            }
            notify();
        },
        increment(productId, name) {
            this.setQuantity(productId, name, this.getQuantity(productId) + 1);
        },
        decrement(productId, name) {
            this.setQuantity(productId, name, this.getQuantity(productId) - 1);
        },
        clear() {
            items.clear();
            notify();
        },
    };
}

function renderDrawerItems(dom, cart) {
    if (!dom.orderItemsList) return;

    const items = cart.getItems();
    dom.orderItemsList.innerHTML = "";

    if (items.length === 0) {
        const empty = document.createElement("p");
        empty.className = "order-empty-hint";
        empty.textContent = "Todavía no agregaste productos.";
        dom.orderItemsList.appendChild(empty);
        return;
    }

    items.forEach((item) => {
        const row = document.createElement("div");
        row.className = "order-item-row";

        const name = document.createElement("span");
        name.className = "order-item-name";
        name.textContent = item.name;

        const stepper = document.createElement("div");
        stepper.className = "order-stepper";

        const minusBtn = document.createElement("button");
        minusBtn.type = "button";
        minusBtn.textContent = "−";
        minusBtn.addEventListener("click", () => cart.decrement(item.productId, item.name));

        const qty = document.createElement("span");
        qty.className = "order-stepper-qty";
        qty.textContent = String(item.quantity);

        const plusBtn = document.createElement("button");
        plusBtn.type = "button";
        plusBtn.textContent = "+";
        plusBtn.addEventListener("click", () => cart.increment(item.productId, item.name));

        stepper.appendChild(minusBtn);
        stepper.appendChild(qty);
        stepper.appendChild(plusBtn);

        row.appendChild(name);
        row.appendChild(stepper);
        dom.orderItemsList.appendChild(row);
    });
}

function updateBadge(dom, cart) {
    if (!dom.orderBadge || !dom.orderBadgeCount) return;
    const total = cart.getTotalQuantity();
    dom.orderBadgeCount.textContent = String(total);
    dom.orderBadge.style.display = total > 0 ? "flex" : "none";
    if (dom.orderCheckoutBtn) dom.orderCheckoutBtn.disabled = total === 0;
}

async function shareOrDownload(blob, fileName) {
    const file = new File([blob], fileName, {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });

    if (navigator.canShare && navigator.canShare({ files: [file] })) {
        try {
            await navigator.share({ files: [file], title: "Pedido" });
            return;
        } catch (e) {
            // el usuario cerró el selector de compartir sin elegir nada — no es un error
            return;
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

function setupOrderUi(dom, cart) {
    if (!dom.orderBadge) return;

    dom.orderBadge.addEventListener("click", () => {
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

    if (dom.orderCheckoutForm) {
        dom.orderCheckoutForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            if (dom.orderFormError) dom.orderFormError.textContent = "";

            const form = dom.orderCheckoutForm;
            const customer = Object.fromEntries(new FormData(form));
            const items = cart.getItems().map((i) => ({ productId: i.productId, quantity: i.quantity }));

            if (dom.orderSubmitBtn) dom.orderSubmitBtn.disabled = true;

            try {
                const response = await fetch("/api/orders/excel", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ companyId: dom.companyId, items, customer }),
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
                if (dom.orderSubmitBtn) dom.orderSubmitBtn.disabled = false;
            }
        });
    }

    cart.subscribe(() => {
        updateBadge(dom, cart);
        renderDrawerItems(dom, cart);
    });

    updateBadge(dom, cart);
    renderDrawerItems(dom, cart);
}

export function initOrderCart(dom) {
    const cart = createCartState(dom);
    setupOrderUi(dom, cart);
    return cart;
}
