export function setupViewerModeSwitch({ flipbookViewer, zoomControl }) {
    zoomControl.onArmedChange((armed) => flipbookViewer.setInteractive(!armed));
}
