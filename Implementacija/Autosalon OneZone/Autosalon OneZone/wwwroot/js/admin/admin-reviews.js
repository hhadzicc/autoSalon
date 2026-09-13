(() => {
    const initialize = () => {
        const root = document.getElementById("recenzije-list-container");
        if (!root || root.dataset.initialized === "true") {
            return;
        }
        root.dataset.initialized = "true";

        const text = JSON.parse(root.dataset.texts || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#recenzija-table-body");
        const $mobileList = $("#recenzije-mobile-list");
        const $userFilter = $("#recenzija-user-filter");
        const $vehicleFilter = $("#recenzija-vozilo-filter");
        const $textSearch = $("#recenzija-text-search");
        const $searchButton = $("#recenzija-filter-search-button");

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

        const reviewDataAttributes = (review) => {
            const values = {
                komentar: review.komentar || "",
                korisnik: review.korisnikUserName || review.korisnikIme || text.notAvailable,
                vozilo: vehicleName(review),
                ocjena: `${review.ocjena || ""} / 5`,
                datum: formatDate(review.datumRecenzije)
            };

            return Object.entries(values)
                .map(([key, value]) => `data-${key}="${escapeAttribute(encodeURIComponent(value))}"`)
                .join(" ");
        };

        const viewButton = (review) => `<button class="btn btn-sm btn-outline-primary rounded-3 view-komentar" ${reviewDataAttributes(review)}>`
            + `<i class="bi bi-eye me-1"></i>${escapeHtml(text.show)}</button>`;

        const reviewActions = (review) => {
            const item = `${vehicleName(review)} · ${text.ratingWord} ${review.ocjena || text.notAvailable}`;
            return `<div class="table-actions">${viewButton(review)}`
                + `<button class="delete-recenzija-button btn btn-sm btn-outline-danger rounded-3" data-id="${review.recenzijaID}" `
                + `data-confirm-delete="true" data-confirm-title="${escapeAttribute(text.deleteTitle)}" `
                + `data-confirm-message="${escapeAttribute(text.deleteMessage)}" data-confirm-item="${escapeAttribute(item)}" `
                + `data-confirm-action="${escapeAttribute(text.deleteAction)}"><i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;
        };

        const renderReviews = (reviews) => {
            $tableBody.empty();
            $mobileList.empty();
            if (!reviews?.length) {
                const empty = `<div class="empty-state"><i class="bi bi-star"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="5">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            $.each(reviews, (_, review) => {
                const rawUser = review.korisnikUserName || review.korisnikIme || text.notAvailable;
                const rawVehicle = vehicleName(review);
                const user = escapeHtml(rawUser);
                const vehicle = escapeHtml(rawVehicle);
                const date = escapeHtml(formatDate(review.datumRecenzije));
                const actions = reviewActions(review);

                $tableBody.append(`<tr><td class="date-cell">${date}</td>`
                    + `<td><div class="user-cell" title="${escapeAttribute(rawUser)}"><span class="user-avatar"><i class="bi bi-person-fill"></i></span><strong>${user}</strong></div></td>`
                    + `<td><div class="vehicle-name-cell" title="${escapeAttribute(rawVehicle)}"><strong>${vehicle}</strong></div></td>`
                    + `<td>${ratingBadge(review.ocjena)}</td><td>${actions}</td></tr>`);

                $mobileList.append(`<article class="admin-review-card"><div class="mobile-card-top"><span class="date-cell">${date}</span>`
                    + `${ratingBadge(review.ocjena)}</div><h3>${vehicle}</h3><p class="mobile-user">${user}</p>`
                    + `<div class="mobile-actions">${actions}</div></article>`);
            });
        };

        const currentFilters = () => ({
            searchQuery: $textSearch.val(),
            korisnikFilter: $userFilter.val(),
            voziloFilter: $vehicleFilter.val()
        });

        const loadReviews = () => {
            $tableBody.html(`<tr><td colspan="5">${escapeHtml(text.loading)}</td></tr>`);
            $mobileList.html(`<div class="empty-state"><i class="bi bi-hourglass-split"></i><p>${escapeHtml(text.loading)}</p></div>`);
            window.appApi.get(root.dataset.listUrl, currentFilters())
                .done((data) => renderReviews(data?.recenzije || []))
                .fail((xhr) => {
                    const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                    const error = `<div class="alert alert-danger mb-0">${escapeHtml(text.loadError)} ${message}</div>`;
                    $tableBody.html(`<tr><td colspan="5">${error}</td></tr>`);
                    $mobileList.html(error);
                });
        };

        $searchButton.off("click.adminReviews").on("click.adminReviews", loadReviews);
        $(root).find('.admin-filter-form input[type="text"]')
            .off("keydown.adminReviews")
            .on("keydown.adminReviews", (event) => {
                if (event.key === "Enter") {
                    event.preventDefault();
                    loadReviews();
                }
            });

        $(document).off("click.adminReviews", ".view-komentar")
            .on("click.adminReviews", ".view-komentar", function (event) {
                event.preventDefault();
                const value = (name, fallback = "-") => decodeURIComponent($(this).attr(`data-${name}`) || fallback);
                $("#komentarModalText").text(value("komentar", ""));
                $("#komentarModalKorisnik").text(value("korisnik"));
                $("#komentarModalVozilo").text(value("vozilo"));
                $("#komentarModalOcjena").text(value("ocjena"));
                $("#komentarModalDatum").text(value("datum"));
                bootstrap.Modal.getOrCreateInstance(document.getElementById("komentarModal")).show();
            });

        $(document).off("app:delete-confirmed.adminReviews", ".delete-recenzija-button")
            .on("app:delete-confirmed.adminReviews", ".delete-recenzija-button", function () {
                window.appApi.post(root.dataset.deleteUrl, { id: $(this).data("id") }).done((response) => {
                    window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                    loadReviews();
                }).fail(() => window.showAppToast(text.deleteError, "error"));
            });

        loadReviews();
    };

    $(document).on("admin:section-loaded.adminReviews", (_, sectionName) => {
        if (sectionName === "Recenzije") initialize();
    });
})();
