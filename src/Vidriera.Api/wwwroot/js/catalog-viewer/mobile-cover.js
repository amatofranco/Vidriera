const SWIPE_THRESHOLD_PX = 40;
const SLIDE_DURATION_MS = 280;

const ARROW_SVG = `<svg class="mobile-cover-arrow" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path d="M5 12h13M13 6l6 6-6 6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>`;

function clampProgress(value) {
    return Math.min(Math.max(value, 0), 1);
}

export function createMobileCover({ dom, viewport, onChange }) {
    const heading = dom.coverInfoEl?.querySelector(".cover-info-logo, .cover-info-company");
    if (!heading) return null;

    const el = document.createElement("div");
    el.id = "mobile-cover";

    const content = document.createElement("div");
    content.className = "mobile-cover-content";
    content.appendChild(heading.cloneNode(true));
    if (dom.coverInfoMetaEl) {
        [...dom.coverInfoMetaEl.children].forEach((child) => content.appendChild(child.cloneNode(true)));
    }

    const hint = document.createElement("button");
    hint.type = "button";
    hint.className = "mobile-cover-hint";
    hint.innerHTML = `<span>Deslizar</span>${ARROW_SVG}`;

    el.append(content, hint);
    dom.stageEl.appendChild(el);

    let shown = true;
    let settleTimer = null;

    function render(progress, animate) {
        clearTimeout(settleTimer);
        const transition = animate ? `transform ${SLIDE_DURATION_MS}ms ease` : "none";
        el.style.visibility = "";
        el.style.transition = transition;
        viewport.style.transition = transition;
        el.style.transform = `translateX(${-progress * 100}vw)`;
        viewport.style.transform = `translateX(${(1 - progress) * 100}vw)`;

        const settle = () => {
            viewport.style.transition = "";
            if (progress === 1) {
                viewport.style.transform = "";
                el.style.visibility = "hidden";
            }
        };
        if (animate) settleTimer = setTimeout(settle, SLIDE_DURATION_MS + 30);
        else settle();
    }

    function setShown(value) {
        if (shown === value) return;
        shown = value;
        dom.stageEl.classList.toggle("on-cover", shown);
        onChange();
    }

    function reveal() {
        render(1, true);
        setShown(false);
    }

    function show() {
        render(0, true);
        setShown(true);
    }

    let startX = null;
    let dx = 0;

    el.addEventListener("touchstart", (e) => {
        if (!shown) return;
        startX = e.touches[0].clientX;
        dx = 0;
    }, { passive: true });

    el.addEventListener("touchmove", (e) => {
        if (startX === null) return;
        dx = e.touches[0].clientX - startX;
        render(clampProgress(-dx / window.innerWidth), false);
    }, { passive: true });

    el.addEventListener("touchend", () => {
        if (startX === null) return;
        startX = null;
        if (-dx > SWIPE_THRESHOLD_PX) reveal();
        else render(0, true);
    });

    el.addEventListener("click", () => {
        if (shown) reveal();
    });

    dom.stageEl.classList.add("on-cover");
    render(0, false);

    return {
        isShown: () => shown,
        hideInstantly() {
            render(1, false);
            setShown(false);
        },
        dragBack(offset) {
            render(clampProgress(1 - offset / window.innerWidth), false);
        },
        releaseBack(offset) {
            if (offset > SWIPE_THRESHOLD_PX) show();
            else render(1, true);
        },
    };
}
