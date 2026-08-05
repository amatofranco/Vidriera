import { renderPageToDataUrl } from "./pdf-render.js";

const NEARBY_MARGIN = 3;
const BACKGROUND_FILL_CONCURRENCY = 2;

export function createLazyPageRenderer(doc, targetPixelWidth) {
    const pageCount = doc.numPages;
    const imgElements = new Array(pageCount);
    const renderedOrRendering = new Set();

    function buildPageDivs() {
        const divs = [];
        for (let i = 0; i < pageCount; i++) {
            const div = document.createElement("div");
            div.className = "page-content";
            const img = document.createElement("img");
            div.appendChild(img);
            imgElements[i] = img;
            divs.push(div);
        }
        return divs;
    }

    async function renderPage(pageNumber) {
        const index = pageNumber - 1;
        if (renderedOrRendering.has(index)) return;
        renderedOrRendering.add(index);
        const dataUrl = await renderPageToDataUrl(doc, pageNumber, targetPixelWidth);
        imgElements[index].src = dataUrl;
    }

    async function ensureRendered(pageNumbers) {
        const pending = pageNumbers.filter(
            (n) => n >= 1 && n <= pageCount && !renderedOrRendering.has(n - 1)
        );
        await Promise.all(pending.map(renderPage));
    }

    function fillRemainingInBackground() {
        let nextPageNumber = 1;
        async function worker() {
            while (nextPageNumber <= pageCount) {
                const pageNumber = nextPageNumber++;
                await renderPage(pageNumber);
            }
        }
        Array.from({ length: BACKGROUND_FILL_CONCURRENCY }, worker);
    }

    function nearbyRange(currentPage) {
        const from = Math.max(1, currentPage - NEARBY_MARGIN);
        const to = Math.min(pageCount, currentPage + NEARBY_MARGIN);
        const pages = [];
        for (let p = from; p <= to; p++) {
            pages.push(p);
        }
        return pages;
    }

    return { pageCount, buildPageDivs, ensureRendered, fillRemainingInBackground, nearbyRange };
}
