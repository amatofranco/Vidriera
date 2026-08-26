import { renderDrawerItems, renderDrawerTotal, updateBadge } from "./order-cart-ui.js";
import { ARGENTINA_PROVINCES } from "./argentina-provinces.js";
import { VAT_CONDITIONS } from "./vat-conditions.js";

function appendOptions(select, values) {
    const placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = "Seleccionar...";
    placeholder.selected = true;
    select.appendChild(placeholder);

    values.forEach((value) => {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = value;
        select.appendChild(option);
    });
}

function buildFieldInput(field) {
    if (field.fieldType === "Province") {
        const select = document.createElement("select");
        appendOptions(select, ARGENTINA_PROVINCES);
        return select;
    }

    if (field.fieldType === "VatCondition") {
        const select = document.createElement("select");
        appendOptions(select, VAT_CONDITIONS);
        return select;
    }

    const input = document.createElement("input");
    input.type = field.fieldType === "Email" ? "email" : "text";
    input.autocomplete = "off";

    if (field.fieldType === "Cuit") {
        input.pattern = "\\d{11}";
        input.maxLength = 11;
        input.inputMode = "numeric";
        input.title = "11 números, sin guiones ni espacios";
    } else if (field.fieldType === "Name") {
        input.pattern = "[\\p{L}\\s'-]+";
        input.title = "Solo letras, sin números";
    }

    return input;
}

function renderOrderFields(dom) {
    if (!dom.orderCheckoutFields) return;
    dom.orderCheckoutFields.innerHTML = "";

    dom.orderFormFields.forEach((field) => {
        const label = document.createElement("label");
        label.append(field.label);

        const input = buildFieldInput(field);
        input.name = field.id;
        if (field.isRequired) {
            input.required = true;
        } else if (input.tagName === "INPUT") {
            input.placeholder = "Opcional";
        }

        label.appendChild(input);
        dom.orderCheckoutFields.appendChild(label);
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
                return;
            }
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

    renderOrderFields(dom);

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
            const formData = new FormData(form);
            const customerFields = dom.orderFormFields.map((field) => ({
                fieldId: field.id,
                value: formData.get(field.id) || "",
            }));
            const items = cart.getItems().map((i) => ({ itemId: i.itemId, quantity: i.quantity }));
            const showPrices = cart.getTotalPrice() !== null;

            if (dom.orderSubmitBtn) {
                dom.orderSubmitBtn.disabled = true;
                dom.orderSubmitBtn.innerHTML = '<span class="order-btn-spinner"></span>Generando pedido...';
            }

            try {
                const response = await fetch("/api/orders/excel", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ companyId: dom.companyId, items, customerFields, showPrices }),
                });

                if (!response.ok) {
                    let message = `Error ${response.status}`;
                    try {
                        const body = await response.json();
                        message = body.detail || body.title || message;
                    } catch (parseError) {
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
