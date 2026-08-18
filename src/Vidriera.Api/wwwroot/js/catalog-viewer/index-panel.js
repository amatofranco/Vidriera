import { MOBILE_BREAKPOINT } from "./layout.js";

export function renderIndexPanel({ dom, indexEntries, onJumpToPage, rebuildRef }) {
    if (!dom.indexList || !indexEntries || indexEntries.length === 0) return;

    const n = indexEntries.length;
    const hasChildren = indexEntries.map((entry, i) => i + 1 < n && indexEntries[i + 1].level > entry.level);
    const expanded = indexEntries.map(() => false);

    dom.indexList.innerHTML = "";
    const rowEls = [];
    const chevronEls = [];
    const orderControls = [];

    indexEntries.forEach((entry, i) => {
        const row = document.createElement("div");
        row.className = "index-row";
        row.dataset.level = String(entry.level);

        const chevron = document.createElement("button");
        chevron.type = "button";
        if (hasChildren[i]) {
            chevron.className = "index-chevron";
            chevron.textContent = "▸";
            chevron.addEventListener("click", () => {
                expanded[i] = !expanded[i];
                rerender();
            });
        } else {
            chevron.className = "index-chevron-spacer";
            chevron.disabled = true;
            chevron.tabIndex = -1;
        }

        const label = document.createElement("button");
        label.type = "button";
        label.className =
            "index-item" +
            (entry.level >= 1 ? " index-item-sub" : "") +
            (entry.isProduct ? " index-item-product" : "");
        label.textContent = entry.name;
        label.addEventListener("click", () => {
            onJumpToPage(entry.startPage);
            // En mobile el índice es un overlay a pantalla completa que tapa el
            // catálogo — a diferencia del sidebar de desktop, acá conviene
            // cerrarlo solo al elegir un producto en vez de obligar a cerrarlo
            // a mano para ver a dónde saltó.
            if (window.innerWidth <= MOBILE_BREAKPOINT && dom.indexPanel) {
                dom.indexPanel.classList.add("closed");
                if (rebuildRef) rebuildRef.current();
            }
        });

        row.appendChild(chevron);
        row.appendChild(label);

        if (entry.isProduct && entry.productId && dom.orderCart) {
            const controlEl = document.createElement("div");
            controlEl.className = "order-add-control";
            row.appendChild(controlEl);
            orderControls.push({ entry, controlEl });
        }

        dom.indexList.appendChild(row);
        rowEls.push(row);
        chevronEls.push(chevron);
    });

    function renderOrderControls() {
        orderControls.forEach(({ entry, controlEl }) => {
            const quantity = dom.orderCart.getQuantity(entry.productId);
            controlEl.innerHTML = "";

            if (quantity === 0) {
                const addBtn = document.createElement("button");
                addBtn.type = "button";
                addBtn.className = "order-add-btn";
                addBtn.title = "Agregar al pedido";
                addBtn.textContent = "+";
                addBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    dom.orderCart.increment(entry.productId, entry.name);
                });
                controlEl.appendChild(addBtn);
                return;
            }

            const stepper = document.createElement("div");
            stepper.className = "order-stepper order-stepper-compact";

            const minusBtn = document.createElement("button");
            minusBtn.type = "button";
            minusBtn.textContent = "−";
            minusBtn.addEventListener("click", (e) => {
                e.stopPropagation();
                dom.orderCart.decrement(entry.productId, entry.name);
            });

            const qtyEl = document.createElement("span");
            qtyEl.className = "order-stepper-qty";
            qtyEl.textContent = String(quantity);

            const plusBtn = document.createElement("button");
            plusBtn.type = "button";
            plusBtn.textContent = "+";
            plusBtn.addEventListener("click", (e) => {
                e.stopPropagation();
                dom.orderCart.increment(entry.productId, entry.name);
            });

            stepper.appendChild(minusBtn);
            stepper.appendChild(qtyEl);
            stepper.appendChild(plusBtn);
            controlEl.appendChild(stepper);
        });
    }

    if (orderControls.length > 0) {
        renderOrderControls();
        dom.orderCart.subscribe(renderOrderControls);
    }

    function subtreeMatchFor(query) {
        const selfMatch = indexEntries.map((e) => e.name.toLowerCase().includes(query));
        const subtreeMatch = selfMatch.slice();
        const lastIndexAtLevel = [];
        for (let i = 0; i < n; i++) {
            const level = indexEntries[i].level;
            lastIndexAtLevel[level] = i;
            if (selfMatch[i]) {
                for (let l = level - 1; l >= 0; l--) {
                    if (lastIndexAtLevel[l] !== undefined) subtreeMatch[lastIndexAtLevel[l]] = true;
                }
            }
        }
        return subtreeMatch;
    }

    function computeVisible(isExpandedAt, isIncludedAt) {
        const visible = new Array(n).fill(false);
        const stack = [];
        for (let i = 0; i < n; i++) {
            const level = indexEntries[i].level;
            while (stack.length && stack[stack.length - 1].level >= level) stack.pop();
            const parent = stack[stack.length - 1];
            const ancestorsVisible = parent ? parent.ancestorsVisible && parent.expanded : true;
            visible[i] = ancestorsVisible && isIncludedAt(i);
            stack.push({ level, ancestorsVisible, expanded: isExpandedAt(i) });
        }
        return visible;
    }

    function rerender() {
        const query = dom.indexSearchInput ? dom.indexSearchInput.value.trim().toLowerCase() : "";
        const searching = query.length > 0;
        const subtreeMatch = searching ? subtreeMatchFor(query) : null;

        const isExpandedAt = searching ? (i) => subtreeMatch[i] : (i) => expanded[i];
        const isIncludedAt = searching ? (i) => subtreeMatch[i] : () => true;

        const visible = computeVisible(isExpandedAt, isIncludedAt);

        for (let i = 0; i < n; i++) {
            rowEls[i].style.display = visible[i] ? "flex" : "none";
            if (hasChildren[i]) {
                chevronEls[i].classList.toggle("expanded", isExpandedAt(i));
            }
        }
    }

    if (dom.indexSearchInput) {
        let wasSearching = false;
        dom.indexSearchInput.addEventListener("input", () => {
            const query = dom.indexSearchInput.value.trim();
            if (wasSearching && query.length === 0) {
                expanded.fill(false);
            }
            wasSearching = query.length > 0;
            rerender();
        });
    }

    rerender();
}
