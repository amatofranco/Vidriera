import { createCartState } from "./order-cart-state.js";
import { renderPageOrderBar } from "./order-cart-ui.js";
import { setupOrderUi } from "./order-checkout.js";

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
