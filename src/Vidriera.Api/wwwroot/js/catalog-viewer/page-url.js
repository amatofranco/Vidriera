export function buildPageUrl(dom, pageNumber) {
    return dom.pagesPublic ? `${dom.pagesBaseUrl}/${pageNumber}.jpg` : `${dom.pagesBaseUrl}/${pageNumber}`;
}
