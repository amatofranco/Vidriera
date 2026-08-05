const LENS_HEADROOM = 1.6;
const MAX_PIXEL_WIDTH = 3400;

export function computeTargetPixelWidth(targetCssWidth) {
    const dpr = window.devicePixelRatio || 1;
    return Math.min(targetCssWidth * dpr * LENS_HEADROOM, MAX_PIXEL_WIDTH);
}

export async function renderPageToDataUrl(doc, pageNumber, targetPixelWidth) {
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
    return canvas.toDataURL("image/png");
}
