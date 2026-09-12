(() => {
    const grid = document.getElementById("vehicles-cards-grid");
    if (!grid) return;

    const sentinel = document.querySelector("[data-vehicles-sentinel]");
    const loader = document.querySelector("[data-vehicles-loader]");
    const storageKey = "onezone.vehicleCatalogState";
    let isLoading = false;
    let nextPage = Number(grid.dataset.nextPage || 2);
    let observer = null;

    const readCatalogState = () => {
        try {
            return JSON.parse(sessionStorage.getItem(storageKey));
        } catch {
            sessionStorage.removeItem(storageKey);
            return null;
        }
    };

    const clearCatalogState = () => sessionStorage.removeItem(storageKey);

    const saveCatalogState = () => {
        sessionStorage.setItem(storageKey, JSON.stringify({
            url: window.location.href,
            scrollY: window.scrollY,
            loadedPage: Math.max(1, nextPage - 1),
            savedAt: Date.now()
        }));
    };

    document.addEventListener("click", event => {
        if (event.target.closest("[data-vehicle-details-link]")) {
            saveCatalogState();
        }
    });

    const stopLoading = () => {
        observer?.disconnect();
        sentinel?.remove();
        loader?.remove();
        grid.dataset.hasMore = "false";
    };

    const loadNextPage = () => {
        if (isLoading || grid.dataset.hasMore !== "true") {
            return Promise.resolve(false);
        }

        isLoading = true;
        if (loader) loader.hidden = false;

        const requestUrl = new URL(grid.dataset.loadUrl, window.location.origin);
        const currentParameters = new URLSearchParams(window.location.search);
        currentParameters.delete("page");
        currentParameters.forEach((value, key) => requestUrl.searchParams.append(key, value));
        requestUrl.searchParams.set("page", String(nextPage));

        return new Promise(resolve => {
            $.ajax({
                url: requestUrl.toString(),
                type: "GET",
                dataType: "html",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).done((html, _status, xhr) => {
                const hasMore = xhr.getResponseHeader("X-Has-More") === "true";
                const cards = html.trim();

                if (cards) {
                    grid.insertAdjacentHTML("beforeend", cards);
                    nextPage += 1;
                    grid.dataset.nextPage = String(nextPage);
                }

                grid.dataset.hasMore = String(hasMore);
                if (!hasMore || !cards) stopLoading();
                resolve(Boolean(cards));
            }).fail(() => {
                resolve(false);
            }).always(() => {
                isLoading = false;
                if (loader?.isConnected) loader.hidden = true;
            });
        });
    };

    const restoreCatalogState = async () => {
        const state = readCatalogState();
        if (!state || state.url !== window.location.href) return;

        const targetPage = Math.max(1, Number(state.loadedPage) || 1);
        while (nextPage <= targetPage && grid.dataset.hasMore === "true") {
            const loaded = await loadNextPage();
            if (!loaded) break;
        }

        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                window.scrollTo(0, Math.max(0, Number(state.scrollY) || 0));
                clearCatalogState();
            });
        });
    };

    const startObserver = () => {
        if (!sentinel || grid.dataset.hasMore !== "true") return;

        observer = new IntersectionObserver(entries => {
            if (!entries.some(entry => entry.isIntersecting)) return;

            loadNextPage().then(loaded => {
                if (!loaded && sentinel.isConnected && grid.dataset.hasMore === "true") {
                    observer.unobserve(sentinel);
                    window.setTimeout(() => observer?.observe(sentinel), 1200);
                }
            });
        }, {
            rootMargin: "400px 0px",
            threshold: 0
        });

        observer.observe(sentinel);
    };

    window.addEventListener("pageshow", event => {
        if (!event.persisted) return;

        const state = readCatalogState();
        if (!state || state.url !== window.location.href) return;

        window.scrollTo(0, Math.max(0, Number(state.scrollY) || 0));
        clearCatalogState();
    });

    restoreCatalogState().finally(startObserver);
})();
