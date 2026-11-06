import { useEffect } from 'react';

export const PRODUCT_NAME = 'Portfolio Manager';

/** Puts the page's name in the browser tab: «Довгострокові інвестиції · Portfolio Manager». */
export function usePageTitle(title: string | undefined) {
    useEffect(() => {
        document.title = title ? `${title} · ${PRODUCT_NAME}` : PRODUCT_NAME;
    }, [title]);
}
