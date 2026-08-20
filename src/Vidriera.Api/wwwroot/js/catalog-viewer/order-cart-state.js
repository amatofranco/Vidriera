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

export function createCartState(dom) {
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
