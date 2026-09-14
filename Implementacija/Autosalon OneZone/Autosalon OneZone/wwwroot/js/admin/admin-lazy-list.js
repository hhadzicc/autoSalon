(() => {
    if (window.adminLazyList) return;

    const create = ({ element, loadMoreText, onLoadMore }) => {
        const $element = $(element);
        let totalCount = 0;
        let loadedCount = 0;
        let hasMore = false;
        let loading = false;

        const render = () => {
            $element.empty();
            if (!hasMore) return;

            $element.append(`<button type="button" class="btn btn-outline-primary admin-load-more-button">`
                + `<i class="bi bi-plus-lg me-2" aria-hidden="true"></i>${window.adminCore.escapeHtml(loadMoreText)}</button>`);
        };

        $element.on("click.adminLazyList", ".admin-load-more-button", () => {
            if (!loading && hasMore) {
                onLoadMore(loadedCount);
            }
        });

        return {
            reset() {
                totalCount = 0;
                loadedCount = 0;
                hasMore = false;
                loading = false;
                $element.empty();
            },
            update(pageInfo, receivedCount, append) {
                totalCount = Number(pageInfo?.totalCount || 0);
                loadedCount = append
                    ? loadedCount + Number(receivedCount || 0)
                    : Number(receivedCount || 0);
                hasMore = typeof pageInfo?.hasMore === "boolean"
                    ? pageInfo.hasMore
                    : loadedCount < totalCount;
                loading = false;
                render();
            },
            setLoading(value) {
                loading = Boolean(value);
                $element.find(".admin-load-more-button")
                    .prop("disabled", loading)
                    .toggleClass("is-loading", loading);
            },
            removeItem() {
                totalCount = Math.max(0, totalCount - 1);
                loadedCount = Math.max(0, loadedCount - 1);
                hasMore = loadedCount < totalCount;
                render();
            }
        };
    };

    window.adminLazyList = { create };
})();
