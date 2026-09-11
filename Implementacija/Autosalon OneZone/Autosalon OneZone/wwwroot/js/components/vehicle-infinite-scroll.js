(() => {
    const grid = document.getElementById("vehicles-cards-grid");
    const sentinel = document.querySelector("[data-vehicles-sentinel]");
    const loader = document.querySelector("[data-vehicles-loader]");

    if (!grid || !sentinel || grid.dataset.hasMore !== "true") {
        return;
    }

    let isLoading = false;
    let nextPage = Number(grid.dataset.nextPage || 2);

    const stopLoading = () => {
        observer.disconnect();
        sentinel.remove();
        loader?.remove();
        grid.dataset.hasMore = "false";
    };

    const loadNextPage = () => {
        if (isLoading || grid.dataset.hasMore !== "true") {
            return;
        }

        isLoading = true;
        if (loader) loader.hidden = false;

        const requestUrl = new URL(grid.dataset.loadUrl, window.location.origin);
        const currentParameters = new URLSearchParams(window.location.search);
        currentParameters.delete("page");
        currentParameters.forEach((value, key) => requestUrl.searchParams.append(key, value));
        requestUrl.searchParams.set("page", String(nextPage));

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
        }).fail(() => {
            observer.unobserve(sentinel);
            window.setTimeout(() => observer.observe(sentinel), 1200);
        }).always(() => {
            isLoading = false;
            if (loader?.isConnected) loader.hidden = true;
        });
    };

    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) loadNextPage();
    }, {
        rootMargin: "400px 0px",
        threshold: 0
    });

    observer.observe(sentinel);
})();
