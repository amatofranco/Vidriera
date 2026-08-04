export async function renderAllPages(doc, targetCssWidth, dom) {
    const dpr = window.devicePixelRatio || 1;
    const LENS_HEADROOM = 1.6;
    const targetPixelWidth = Math.min(targetCssWidth * dpr * LENS_HEADROOM, 3400);
    const images = new Array(doc.numPages);
    let renderedCount = 0;

    async function renderPage(pageNumber) {
        const page = await doc.getPage(pageNumber);
        const naturalViewport = page.getViewport({ scale: 1 });
        const scale = targetPixelWidth / naturalViewport.width;
        const viewport = page.getViewport({ scale });
        const canvas = document.createElement("canvas");
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        await page.render({ canvasContext: ctx, viewport }).promise;
        images[pageNumber - 1] = canvas.toDataURL("image/png");
        renderedCount++;
        dom.loadingTextEl.textContent = `Preparando catálogo... (${renderedCount}/${doc.numPages})`;
    }

    const RENDER_CONCURRENCY = 4;
    let nextPageNumber = 1;
    async function renderWorker() {
        while (nextPageNumber <= doc.numPages) {
            const pageNumber = nextPageNumber++;
            await renderPage(pageNumber);
        }
    }
    await Promise.all(
        Array.from({ length: Math.min(RENDER_CONCURRENCY, doc.numPages) }, renderWorker)
    );

    return images;
}
