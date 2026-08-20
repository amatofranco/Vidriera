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
                price: entry.price ?? null,
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
        // El total solo se calcula si TODOS los items tienen precio — vienen
        // del mismo catálogo generado, así que si a uno le falta es porque el
        // catálogo se generó sin precios y no hay que sumar nada.
        getTotalPrice() {
            let total = 0;
            let hasAnyPrice = false;
            for (const entry of items.values()) {
                if (entry.price == null) return null;
                hasAnyPrice = true;
                total += entry.price * entry.quantity;
            }
            return hasAnyPrice ? total : null;
        },
        // price es opcional: al cambiar solo la cantidad (drawer) no viene, y
        // se preserva el precio que ya estaba guardado en vez de perderlo.
        setQuantity(productId, name, quantity, price) {
            if (quantity <= 0) {
                items.delete(productId);
            } else {
                const existingPrice = items.get(productId)?.price;
                items.set(productId, { name, quantity, price: price !== undefined ? price : existingPrice });
            }
            notify();
        },
        increment(productId, name, price) {
            this.setQuantity(productId, name, this.getQuantity(productId) + 1, price);
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

// Botón "+" cuando todavía no se agregó nada, o stepper "− [n] +" una vez
// que hay cantidad — mismo control visual para el drawer y para la barra de
// la página actual. El campo de cantidad es un input editable: además de
// +/-, se puede escribir la cantidad directamente.
function createStepperEl(quantity, onMinus, onPlus, onSetQuantity) {
    if (quantity === 0) {
        const addBtn = document.createElement("button");
        addBtn.type = "button";
        addBtn.className = "order-add-btn";
        addBtn.title = "Agregar al pedido";
        addBtn.textContent = "+";
        addBtn.addEventListener("click", onPlus);
        return addBtn;
    }

    const stepper = document.createElement("div");
    stepper.className = "order-stepper";

    const minusBtn = document.createElement("button");
    minusBtn.type = "button";
    minusBtn.textContent = "−";
    minusBtn.addEventListener("click", onMinus);

    const qtyInput = document.createElement("input");
    qtyInput.type = "number";
    qtyInput.className = "order-stepper-qty-input";
    qtyInput.min = "0";
    qtyInput.inputMode = "numeric";
    qtyInput.value = String(quantity);
    qtyInput.addEventListener("click", (e) => e.stopPropagation());
    qtyInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            qtyInput.blur();
        }
    });
    qtyInput.addEventListener("change", () => {
        const parsed = parseInt(qtyInput.value, 10);
        onSetQuantity(Number.isFinite(parsed) && parsed >= 0 ? parsed : quantity);
    });

    const plusBtn = document.createElement("button");
    plusBtn.type = "button";
    plusBtn.textContent = "+";
    plusBtn.addEventListener("click", onPlus);

    stepper.appendChild(minusBtn);
    stepper.appendChild(qtyInput);
    stepper.appendChild(plusBtn);
    return stepper;
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
        row.appendChild(name);

        if (item.price != null) {
            const price = document.createElement("span");
            price.className = "order-item-price";
            price.textContent = priceFormatter.format(item.price);
            row.appendChild(price);
        }

        const stepperEl = createStepperEl(
            item.quantity,
            () => cart.decrement(item.productId, item.name),
            () => cart.increment(item.productId, item.name, item.price),
            (value) => cart.setQuantity(item.productId, item.name, value, item.price)
        );

        row.appendChild(stepperEl);
        dom.orderItemsList.appendChild(row);
    });
}

function renderDrawerTotal(dom, cart) {
    if (!dom.orderDrawerTotal) return;
    const total = cart.getTotalPrice();
    if (total == null) {
        dom.orderDrawerTotal.style.display = "none";
        return;
    }
    dom.orderDrawerTotal.style.display = "flex";
    dom.orderDrawerTotal.innerHTML = "";

    const label = document.createElement("span");
    label.textContent = "Total";
    const amount = document.createElement("span");
    amount.textContent = priceFormatter.format(total);

    dom.orderDrawerTotal.appendChild(label);
    dom.orderDrawerTotal.appendChild(amount);
}

function updateBadge(dom, cart) {
    if (!dom.orderBadgeWrap || !dom.orderBadgeCount) return;
    const total = cart.getTotalQuantity();
    dom.orderBadgeCount.textContent = String(total);
    dom.orderBadgeWrap.style.display = total > 0 ? "flex" : "none";
    if (dom.orderCheckoutBtn) dom.orderCheckoutBtn.disabled = total === 0;
}

// El índice ya sabe en qué página empieza cada producto (entry.startPage);
// como las entradas están en orden de página, el producto "activo" en una
// página dada es la última entrada de producto cuyo startPage no la supera.
function getProductEntryForPage(dom, pageNumber) {
    const entries = dom.sectionsData || [];
    let current = null;
    for (const entry of entries) {
        if (entry.startPage <= pageNumber) current = entry;
        else break;
    }
    return current && current.isProduct && current.productId ? current : null;
}

// Página simple (mobile) o doble página (desktop): junta las entradas de
// producto de cada página visible en este momento, promediando el centerX
// de las páginas que comparten producto (una ficha que ocupa las dos mitades
// de la doble página termina centrada en el medio de la doble página).
function getVisibleProductEntries(dom, pageEntries) {
    const byProduct = new Map();
    pageEntries.forEach(({ pageNumber, centerX }) => {
        const entry = getProductEntryForPage(dom, pageNumber);
        if (!entry) return;
        if (!byProduct.has(entry.productId)) {
            byProduct.set(entry.productId, { entry, centers: [] });
        }
        byProduct.get(entry.productId).centers.push(centerX);
    });
    return Array.from(byProduct.values()).map(({ entry, centers }) => ({
        entry,
        centerX: centers.reduce((sum, c) => sum + c, 0) / centers.length,
    }));
}

const priceFormatter = new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
});

function renderPageOrderBar(dom, cart, pageEntries) {
    if (!dom.pageOrderBar) return;

    const items = getVisibleProductEntries(dom, pageEntries);
    dom.pageOrderBar.innerHTML = "";

    if (items.length === 0) {
        dom.pageOrderBar.style.display = "none";
        return;
    }

    dom.pageOrderBar.style.display = "block";

    items.forEach(({ entry, centerX }) => {
        const chip = document.createElement("div");
        chip.className = "page-order-chip";
        chip.style.left = `${Math.round(centerX)}px`;

        const label = document.createElement("span");
        label.className = "page-order-chip-label";
        label.textContent = "AGREGAR";

        chip.appendChild(label);

        if (entry.price != null) {
            const price = document.createElement("span");
            price.className = "page-order-chip-price";
            price.textContent = priceFormatter.format(entry.price);
            chip.appendChild(price);
        }

        const stepperSlot = document.createElement("div");
        stepperSlot.appendChild(
            createStepperEl(
                cart.getQuantity(entry.productId),
                () => cart.decrement(entry.productId, entry.name),
                () => cart.increment(entry.productId, entry.name, entry.price),
                (value) => cart.setQuantity(entry.productId, entry.name, value, entry.price)
            )
        );

        chip.appendChild(stepperSlot);
        dom.pageOrderBar.appendChild(chip);
    });
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

function setupOrderUi(dom, cart, renderPageBar) {
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

export function initOrderCart(dom) {
    const cart = createCartState(dom);
    let currentPageEntries = [];

    function renderPageBar() {
        renderPageOrderBar(dom, cart, currentPageEntries);
    }

    // pageEntries: [{ pageNumber, centerX }], una por cada página visible
    // (una en mobile, hasta dos en doble página desktop) — el visor calcula
    // centerX a partir del elemento real de esa página para que el chip
    // quede centrado horizontalmente debajo de ella.
    cart.setCurrentPages = (pageEntries) => {
        currentPageEntries = pageEntries;
        renderPageBar();
    };

    setupOrderUi(dom, cart, renderPageBar);
    renderPageBar();

    return cart;
}
