(() => {
    const initialize = () => {
        const root = document.getElementById("recenzije-list-container");
        if (!root || root.dataset.initialized === "true") return;
        root.dataset.initialized = "true";

        const text = JSON.parse(root.dataset.texts || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#recenzija-table-body");
        const $mobileList = $("#recenzije-mobile-list");
        const $userFilter = $("#recenzija-user-filter");
        const $userIdFilter = $("#recenzija-user-id-filter");
        const $vehicleFilter = $("#recenzija-vozilo-filter");
        const $vehicleIdFilter = $("#recenzija-vozilo-id-filter");
        const $ratingFilter = $("#recenzija-rating-filter");
        const $textSearch = $("#recenzija-text-search");
        const $searchButton = $("#recenzija-filter-search-button");
        const $clearAllButton = $("#recenzija-clear-all");
        const sortDefaults = { datum: "desc", ocjena: "desc" };
        let currentSort = "datum";
        let currentDirection = "desc";
        let pendingFocusId = window.location.hash.match(/^#review-(\d+)$/)?.[1] || "";
        let activeListRequest = null;
        const pager = window.adminLazyList.create({
            element: root.querySelector("[data-admin-lazy-controls]"),
            loadMoreText: text.loadMore,
            onLoadMore: (offset) => loadReviews(offset, true, false)
        });

        const formatDate = (dateString) => {
            if (!dateString) return "";
            const date = new Date(dateString);
            if (Number.isNaN(date.getTime())) return "";
            const day = String(date.getDate()).padStart(2, "0");
            const month = String(date.getMonth() + 1).padStart(2, "0");
            return `${day}-${month}-${date.getFullYear()}`;
        };

        const ratingBadge = (value) => {
            const rating = Number(value || 0);
            return `<span class="rating-badge">${rating} <i class="bi bi-star-fill"></i></span>`;
        };

        const vehicleName = (review) => review.voziloNaziv
            || `${review.voziloMarka || ""} ${review.voziloModel || ""}`.trim()
            || text.notAvailable;

        const customerName = (review) => review.korisnikIme
            || review.korisnikUserName
            || review.korisnikEmail
            || text.notAvailable;

        const localUrl = (url) => `${url.pathname}${url.search}${url.hash}`;

        const reviewReturnUrl = (reviewId) => {
            const url = new URL(window.location.href);
            url.hash = `review-${reviewId}`;
            return localUrl(url);
        };

        const customerUrl = (review) => {
            const url = new URL(root.dataset.usersUrl, window.location.origin);
            url.searchParams.set("section", "Profili");
            url.searchParams.set("focusUserId", review.korisnikId);
            url.searchParams.set("returnUrl", reviewReturnUrl(review.recenzijaID));
            return localUrl(url);
        };

        const vehicleUrl = (review) => {
            const base = String(root.dataset.vehicleDetailsUrl || "").replace(/\/$/, "");
            const url = new URL(`${base}/${review.voziloID}`, window.location.origin);
            url.searchParams.set("returnUrl", reviewReturnUrl(review.recenzijaID));
            return localUrl(url);
        };

        const customerDisplay = (review) => {
            const name = customerName(review);
            const title = [review.korisnikIme, review.korisnikUserName, review.korisnikEmail]
                .filter(Boolean)
                .join(" · ");
            const content = `<span class="user-avatar"><i class="bi bi-person-fill"></i></span><strong>${escapeHtml(name)}</strong>`;
            if (root.dataset.canViewUsers !== "true" || !review.korisnikId) {
                return `<div class="user-cell" title="${escapeAttribute(title || name)}">${content}</div>`;
            }
            return `<a class="user-cell admin-entity-link" href="${escapeAttribute(customerUrl(review))}" title="${escapeAttribute(title || name)}">${content}</a>`;
        };

        const vehicleDisplay = (review) => {
            const name = vehicleName(review);
            if (!review.voziloID) {
                return `<div class="vehicle-name-cell" title="${escapeAttribute(name)}"><strong>${escapeHtml(name)}</strong></div>`;
            }
            return `<a class="vehicle-name-cell admin-entity-link" href="${escapeAttribute(vehicleUrl(review))}" title="${escapeAttribute(name)}"><strong>${escapeHtml(name)}</strong></a>`;
        };

        const reviewDataAttributes = (review) => {
            const customer = [customerName(review), review.korisnikEmail].filter(Boolean).join(" · ");
            const values = {
                komentar: review.komentar || "",
                korisnik: customer,
                vozilo: vehicleName(review),
                ocjena: `${review.ocjena || ""} / 5`,
                datum: formatDate(review.datumRecenzije),
                korisnikUrl: root.dataset.canViewUsers === "true" && review.korisnikId ? customerUrl(review) : "",
                voziloUrl: review.voziloID ? vehicleUrl(review) : ""
            };

            return Object.entries(values)
                .map(([key, value]) => `data-${key}="${escapeAttribute(encodeURIComponent(value))}"`)
                .join(" ");
        };

        const viewButton = (review) => `<button class="btn btn-sm btn-outline-primary rounded-3 view-komentar" ${reviewDataAttributes(review)}>`
            + `<i class="bi bi-eye me-1"></i>${escapeHtml(text.view)}</button>`;

        const reviewActions = (review) => {
            const item = `${vehicleName(review)} · ${text.ratingWord} ${review.ocjena || text.notAvailable}`;
            return `<div class="table-actions">${viewButton(review)}`
                + `<button class="delete-recenzija-button btn btn-sm btn-outline-danger rounded-3" data-id="${review.recenzijaID}" `
                + `data-confirm-delete="true" data-confirm-title="${escapeAttribute(text.deleteTitle)}" `
                + `data-confirm-message="${escapeAttribute(text.deleteMessage)}" data-confirm-item="${escapeAttribute(item)}" `
                + `data-confirm-action="${escapeAttribute(text.deleteAction)}"><i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;
        };

        const scrollToFocusedReview = () => {
            if (!pendingFocusId) return;
            const item = root.querySelector(`[data-admin-item-id="${CSS.escape(pendingFocusId)}"]`);
            if (!item) return;
            pendingFocusId = "";
            window.requestAnimationFrame(() => item.scrollIntoView({ behavior: "auto", block: "center" }));
        };

        const renderReviews = (reviews, append = false) => {
            if (!append) {
                $tableBody.empty();
                $mobileList.empty();
            }
            if (!reviews?.length) {
                if (append) return;
                const empty = `<div class="empty-state"><i class="bi bi-star"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="5">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            $.each(reviews, (_, review) => {
                const id = review.recenzijaID;
                const date = escapeHtml(formatDate(review.datumRecenzije));
                const actions = reviewActions(review);
                const customer = customerDisplay(review);
                const vehicle = vehicleDisplay(review);

                $tableBody.append(`<tr id="review-${id}" data-admin-item-id="${id}"><td class="date-cell">${date}</td>`
                    + `<td>${customer}</td><td>${vehicle}</td>`
                    + `<td>${ratingBadge(review.ocjena)}</td><td>${actions}</td></tr>`);

                $mobileList.append(`<article id="review-mobile-${id}" class="admin-review-card" data-admin-item-id="${id}"><div class="mobile-card-top"><span class="date-cell">${date}</span>`
                    + `${ratingBadge(review.ocjena)}</div>${vehicle}<div class="mobile-user">${customer}</div>`
                    + `<div class="mobile-actions">${actions}</div></article>`);
            });

            scrollToFocusedReview();
        };

        const hasActiveFilters = () => Boolean(
            $userIdFilter.val()
            || String($userFilter.val() || "").trim()
            || $vehicleIdFilter.val()
            || String($vehicleFilter.val() || "").trim()
            || $ratingFilter.val()
            || String($textSearch.val() || "").trim()
        );

        const syncClearAll = () => {
            const hasFilters = hasActiveFilters();
            $clearAllButton
                .toggleClass("is-inactive", !hasFilters)
                .prop("disabled", !hasFilters)
                .attr("aria-hidden", String(!hasFilters));
        };

        const nextDirection = (sort) => sort === currentSort
            ? (currentDirection === "desc" ? "asc" : "desc")
            : sortDefaults[sort];

        const updateSortControls = () => {
            $(root).find(".review-sort-link").each(function () {
                const $link = $(this);
                const sort = $link.data("sort-key");
                const active = sort === currentSort;
                $link.toggleClass("active", active);
                $link.find("i").removeClass("bi-arrow-down-up bi-arrow-down-short bi-arrow-up-short")
                    .addClass(active ? (currentDirection === "asc" ? "bi-arrow-up-short" : "bi-arrow-down-short") : "bi-arrow-down-up");
            });
        };

        const updateUrl = (preserveHash = false) => {
            const params = new URLSearchParams();
            params.set("section", "Recenzije");
            if ($userIdFilter.val()) {
                params.set("korisnikIdFilter", $userIdFilter.val());
            } else if ($userFilter.val()) {
                params.set("korisnikFilter", $userFilter.val());
            }
            if ($vehicleIdFilter.val()) {
                params.set("voziloIdFilter", $vehicleIdFilter.val());
            } else if ($vehicleFilter.val()) {
                params.set("voziloFilter", $vehicleFilter.val());
            }
            if ($ratingFilter.val()) params.set("ocjenaFilter", $ratingFilter.val());
            if ($textSearch.val()) params.set("searchQuery", $textSearch.val());
            params.set("sort", currentSort);
            params.set("direction", currentDirection);
            const hash = preserveHash ? window.location.hash : "";
            window.history.replaceState({}, "", `${window.location.pathname}?${params}${hash}`);
            updateSortControls();
        };

        const readInitialState = () => {
            const params = new URLSearchParams(window.location.search);
            if (params.get("section") !== "Recenzije") {
                updateSortControls();
                syncClearAll();
                return;
            }

            window.adminCombobox.setValue(
                $userFilter.closest("[data-admin-combobox]")[0],
                params.get("korisnikIdFilter"),
                params.get("korisnikFilter") || "");
            window.adminCombobox.setValue(
                $vehicleFilter.closest("[data-admin-combobox]")[0],
                params.get("voziloIdFilter"),
                params.get("voziloFilter") || "");
            const rating = params.get("ocjenaFilter");
            if (rating && $ratingFilter.find("option").filter((_, option) => option.value === rating).length) {
                $ratingFilter.val(rating);
            }
            $textSearch.val(params.get("searchQuery") || root.dataset.initialSearch || "");
            window.adminFilterControls.sync(root);
            if (sortDefaults[params.get("sort")]) currentSort = params.get("sort");
            currentDirection = params.get("direction") === "asc" ? "asc" : "desc";
            updateSortControls();
            syncClearAll();
        };

        const currentFilters = () => ({
            searchQuery: $textSearch.val(),
            korisnikIdFilter: $userIdFilter.val(),
            korisnikFilter: $userIdFilter.val() ? "" : $userFilter.val(),
            voziloIdFilter: $vehicleIdFilter.val(),
            voziloFilter: $vehicleIdFilter.val() ? "" : $vehicleFilter.val(),
            ocjenaFilter: $ratingFilter.val(),
            sort: currentSort,
            direction: currentDirection
        });

        const loadReviews = (offset = 0, append = false, syncUrl = true) => {
            activeListRequest?.abort();
            if (syncUrl) {
                pendingFocusId = "";
                updateUrl();
            }
            if (append) {
                pager.setLoading(true);
            } else {
                pager.reset();
                window.adminCore.setLoading(root, true);
            }
            const request = window.appApi.get(root.dataset.listUrl, { ...currentFilters(), offset })
                .done((data) => {
                    const reviews = data?.recenzije || [];
                    renderReviews(reviews, append);
                    pager.update(data, reviews.length, append);
                })
                .fail((xhr, statusText) => {
                    if (statusText === "abort") return;
                    if (append) {
                        window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error");
                        return;
                    }
                    const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                    const error = `<div class="alert alert-danger mb-0">${message}</div>`;
                    $tableBody.html(`<tr><td colspan="5">${error}</td></tr>`);
                    $mobileList.html(error);
                }).always(() => {
                    if (activeListRequest !== request) return;
                    activeListRequest = null;
                    pager.setLoading(false);
                    if (!append) window.adminCore.setLoading(root, false);
                });
            activeListRequest = request;
        };

        $searchButton.off("click.adminReviews").on("click.adminReviews", () => loadReviews());
        $textSearch.off("input.adminReviews keydown.adminReviews")
            .on("input.adminReviews", syncClearAll)
            .on("keydown.adminReviews", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                loadReviews();
            }
        });
        $ratingFilter.off("change.adminReviews").on("change.adminReviews", () => {
            syncClearAll();
            loadReviews();
        });
        $(root).off("input.adminReviewsFilters", "[data-combobox-input]")
            .on("input.adminReviewsFilters", "[data-combobox-input]", syncClearAll)
            .off("admin-combobox:cleared.adminReviews admin-combobox:selected.adminReviews")
            .on("admin-combobox:cleared.adminReviews", "[data-admin-combobox]", syncClearAll)
            .on("admin-combobox:selected.adminReviews", "[data-admin-combobox]", () => {
                syncClearAll();
                loadReviews();
            })
            .off("admin-filter:cleared.adminReviews")
            .on("admin-filter:cleared.adminReviews", syncClearAll);

        $clearAllButton.off("click.adminReviews").on("click.adminReviews", () => {
            window.adminCombobox.setValue($userFilter.closest("[data-admin-combobox]")[0], "", "");
            window.adminCombobox.setValue($vehicleFilter.closest("[data-admin-combobox]")[0], "", "");
            $ratingFilter.val("");
            $textSearch.val("");
            window.adminFilterControls.sync(root);
            syncClearAll();
            loadReviews();
        });

        $(document).off("click.adminReviewsSort", ".review-sort-link")
            .on("click.adminReviewsSort", ".review-sort-link", function (event) {
                event.preventDefault();
                const sort = $(this).data("sort-key");
                if (!sortDefaults[sort]) return;
                currentDirection = nextDirection(sort);
                currentSort = sort;
                loadReviews();
            });

        $(document).off("click.adminReviews", ".view-komentar")
            .on("click.adminReviews", ".view-komentar", function (event) {
                event.preventDefault();
                const value = (name, fallback = "-") => decodeURIComponent($(this).attr(`data-${name}`) || fallback);
                const modalLink = (selector, label, url) => {
                    const $target = $(selector);
                    if (!url) {
                        $target.text(label);
                        return;
                    }
                    $target.html(`<a class="admin-modal-entity-link" href="${escapeAttribute(url)}">${escapeHtml(label)}</a>`);
                };

                $("#komentarModalText").text(value("komentar", ""));
                modalLink("#komentarModalKorisnik", value("korisnik"), value("korisnikUrl", ""));
                modalLink("#komentarModalVozilo", value("vozilo"), value("voziloUrl", ""));
                $("#komentarModalOcjena").text(value("ocjena"));
                $("#komentarModalDatum").text(value("datum"));
                bootstrap.Modal.getOrCreateInstance(document.getElementById("komentarModal")).show();
            });

        $(document).off("app:delete-confirmed.adminReviews", ".delete-recenzija-button")
            .on("app:delete-confirmed.adminReviews", ".delete-recenzija-button", function () {
                const id = $(this).data("id");
                window.appApi.post(root.dataset.deleteUrl, { id }).done((response) => {
                    window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                    window.adminCore.removeRenderedItem(root, id, () => renderReviews([]));
                    pager.removeItem();
                }).fail(() => window.showAppToast(text.deleteError, "error"));
            });

        window.adminCombobox.initialize(root);
        window.adminFilterControls.initialize(root);
        readInitialState();
        updateUrl(Boolean(pendingFocusId));
        loadReviews(0, false, false);
    };

    $(document).on("admin:section-loaded.adminReviews", (_, sectionName) => {
        if (sectionName === "Recenzije") initialize();
    });
})();
