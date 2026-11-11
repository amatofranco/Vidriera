
const TICK_MS = 400;

export function toHdUrl(url) {
    if (!url) return null;
    const publicMatch = url.match(/^(.*\/\d+)\.jpg(\?.*)?$/);
    if (publicMatch) return `${publicMatch[1]}-hd.jpg${publicMatch[2] ?? ""}`;
    const apiMatch = url.match(/^(.*\/pages\/\d+)(\?.*)?$/);
    if (apiMatch) return `${apiMatch[1]}/hd${apiMatch[2] ?? ""}`;
    return null;
}

export function createHdPageUpgrader(getTarget) {
    const failed = new Set();
    const loading = new Map();
    const swapped = new Set();
    let armed = false;
    let timer = null;
    let previouslyVisible = new Set();

    function visiblePageImages() {
        const target = getTarget();
        if (!target) return [];
        const targetRect = target.getBoundingClientRect();
        return Array.from(target.querySelectorAll("img")).filter((img) => {
            if (!img.src || img.offsetParent === null) return false;
            const r = img.getBoundingClientRect();
            return r.width > 0 && r.height > 0
                && r.right > targetRect.left && r.left < targetRect.right
                && r.bottom > targetRect.top && r.top < targetRect.bottom;
        });
    }

    function normalSrcOf(img) {
        return img.dataset.hdNormalSrc && img.src === img.dataset.hdSrc ? img.dataset.hdNormalSrc : img.src;
    }

    function load(hdUrl) {
        if (failed.has(hdUrl)) return Promise.reject();
        if (!loading.has(hdUrl)) {
            const pre = new Image();
            pre.src = hdUrl;
            const promise = pre.decode().catch((e) => {
                failed.add(hdUrl);
                loading.delete(hdUrl);
                throw e;
            });
            loading.set(hdUrl, promise);
        }
        return loading.get(hdUrl);
    }

    function upgrade(img) {
        if (img.dataset.hdSrc && img.src === img.dataset.hdSrc) return;
        const normalSrc = img.src;
        const hdUrl = toHdUrl(normalSrc);
        if (!hdUrl) return;
        load(hdUrl).then(() => {
            if (!armed || img.src !== normalSrc) return;
            img.dataset.hdNormalSrc = normalSrc;
            img.dataset.hdSrc = hdUrl;
            img.src = hdUrl;
            swapped.add(img);
        }, () => {});
    }

    function tick() {
        const visible = visiblePageImages();
        const nowVisible = new Set(visible.map(normalSrcOf));
        visible.filter((img) => previouslyVisible.has(normalSrcOf(img))).forEach(upgrade);
        previouslyVisible = nowVisible;
    }

    return {
        warm() {
            if (armed) return;
            visiblePageImages().forEach((img) => {
                const hdUrl = toHdUrl(normalSrcOf(img));
                if (hdUrl) load(hdUrl).catch(() => {});
            });
        },
        start() {
            if (armed) return;
            armed = true;
            visiblePageImages().forEach(upgrade);
            previouslyVisible = new Set(visiblePageImages().map(normalSrcOf));
            timer = setInterval(tick, TICK_MS);
        },
        stop() {
            armed = false;
            clearInterval(timer);
            timer = null;
            previouslyVisible = new Set();
            swapped.forEach((img) => {
                if (img.src === img.dataset.hdSrc) img.src = img.dataset.hdNormalSrc;
                delete img.dataset.hdSrc;
                delete img.dataset.hdNormalSrc;
            });
            swapped.clear();
        },
    };
}
