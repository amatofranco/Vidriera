import { createCartState } from "./order-cart-state.js";
import { renderPageOrderBar } from "./order-cart-ui.js";
import { setupOrderUi } from "./order-checkout.js";

export function initOrderCart(dom) {
    const cart = createCartState(dom);
    let currentPageEntries = [];

    function renderPageBar() {
        renderPageOrderBar(dom, cart, currentPageEntries);
    }

    cart.setCurrentPages = (pageEntries) => {
        currentPageEntries = pageEntries;
        renderPageBar();
    };

    setupOrderUi(dom, cart, renderPageBar);
    renderPageBar();

    return cart;
}
