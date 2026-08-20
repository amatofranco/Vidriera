const priceFormatter = new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
});

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

export function renderDrawerItems(dom, cart) {
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

export function renderDrawerTotal(dom, cart) {
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

export function updateBadge(dom, cart) {
    if (!dom.orderBadgeWrap || !dom.orderBadgeCount) return;
    const total = cart.getTotalQuantity();
    dom.orderBadgeCount.textContent = String(total);
    dom.orderBadgeWrap.style.display = total > 0 ? "flex" : "none";
    if (dom.orderCheckoutBtn) dom.orderCheckoutBtn.disabled = total === 0;
}

function getProductEntryForPage(dom, pageNumber) {
    const entries = dom.sectionsData || [];
    let current = null;
    for (const entry of entries) {
        if (entry.startPage <= pageNumber) current = entry;
        else break;
    }
    return current && current.isProduct && current.productId ? current : null;
}

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

export function renderPageOrderBar(dom, cart, pageEntries) {
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
