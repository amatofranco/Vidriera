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
            return Array.from(items.entries()).map(([itemId, entry]) => ({
                itemId,
                name: entry.name,
                quantity: entry.quantity,
                price: entry.price ?? null,
            }));
        },
        getQuantity(itemId) {
            return items.get(itemId)?.quantity || 0;
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
        setQuantity(itemId, name, quantity, price) {
            if (quantity <= 0) {
                items.delete(itemId);
            } else {
                const existingPrice = items.get(itemId)?.price;
                items.set(itemId, { name, quantity, price: price !== undefined ? price : existingPrice });
            }
            notify();
        },
        increment(itemId, name, price) {
            this.setQuantity(itemId, name, this.getQuantity(itemId) + 1, price);
        },
        decrement(itemId, name) {
            this.setQuantity(itemId, name, this.getQuantity(itemId) - 1);
        },
        clear() {
            items.clear();
            notify();
        },
    };
}
