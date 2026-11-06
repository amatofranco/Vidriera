const OVERFLOW_RESTORE_DELAY_MS = 350;
let overflowRestoreTimer = null;

export function setPageOverflowHidden(hidden) {
    if (overflowRestoreTimer) {
        clearTimeout(overflowRestoreTimer);
        overflowRestoreTimer = null;
    }

    if (hidden) {
        document.documentElement.style.overflow = "hidden";
        document.body.style.overflow = "hidden";
        return;
    }

    overflowRestoreTimer = setTimeout(() => {
        document.documentElement.style.overflow = "auto";
        document.body.style.overflow = "auto";
        overflowRestoreTimer = null;
    }, OVERFLOW_RESTORE_DELAY_MS);
}
