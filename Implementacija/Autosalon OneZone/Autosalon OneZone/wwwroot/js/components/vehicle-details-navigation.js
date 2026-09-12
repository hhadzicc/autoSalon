(() => {
    const backButton = document.querySelector("[data-vehicle-back]");
    if (!backButton) return;

    const storageKey = "onezone.vehicleCatalogState";

    backButton.addEventListener("click", () => {
        const returnUrl = backButton.dataset.returnUrl;
        if (returnUrl) {
            window.location.assign(returnUrl);
            return;
        }

        const fallbackUrl = backButton.dataset.fallbackUrl || "/Vozilo";
        let catalogState = null;

        try {
            catalogState = JSON.parse(sessionStorage.getItem(storageKey));
        } catch {
            sessionStorage.removeItem(storageKey);
        }

        let referrer = null;
        try {
            referrer = document.referrer ? new URL(document.referrer) : null;
        } catch {
            referrer = null;
        }

        if (referrer?.origin === window.location.origin && referrer.pathname === "/Vozilo") {
            window.history.back();
            return;
        }

        const hasRecentCatalogState = catalogState?.url
            && Date.now() - Number(catalogState.savedAt || 0) < 30 * 60 * 1000;

        if (hasRecentCatalogState) {
            window.location.assign(catalogState.url);
            return;
        }

        window.location.assign(fallbackUrl);
    });
})();
