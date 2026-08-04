const BG_IMG_W = 2400;
const BG_IMG_H = 1570;
const NICHE_LEFT_FRAC = 0.273;
const NICHE_RIGHT_FRAC = 0.739;

export function getNicheMaxWidth() {
    const scale = Math.max(window.innerWidth / BG_IMG_W, window.innerHeight / BG_IMG_H);
    const renderedW = BG_IMG_W * scale;
    return (NICHE_RIGHT_FRAC - NICHE_LEFT_FRAC) * renderedW;
}

export function getNicheRect() {
    const scale = Math.max(window.innerWidth / BG_IMG_W, window.innerHeight / BG_IMG_H);
    const renderedW = BG_IMG_W * scale;
    const offsetX = (window.innerWidth - renderedW) / 2;
    return {
        left: offsetX + NICHE_LEFT_FRAC * renderedW,
        right: offsetX + NICHE_RIGHT_FRAC * renderedW,
    };
}
