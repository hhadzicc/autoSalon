(() => {
    const initializeVehicleForm = () => {
        const form = document.getElementById("add-edit-vozilo-form");
        if (!form || form.dataset.initialized === "true") return;
        form.dataset.initialized = "true";
        const $form = $(form);
        const text = JSON.parse(form.dataset.validationTexts || "{}");
        const yearInput = form.elements.Godiste;
        const minimumVehicleYear = Number(yearInput?.min);
        const maximumVehicleYear = Number(yearInput?.max);

        $form.validate({
            errorElement: "span",
            errorClass: "text-danger",
            highlight: (element) => $(element).addClass("is-invalid").removeClass("is-valid"),
            unhighlight: (element) => $(element).addClass("is-valid").removeClass("is-invalid"),
            errorPlacement: (error, element) => error.insertAfter(element),
            rules: {
                Marka: { required: true, maxlength: 100 },
                Model: { required: true, maxlength: 100 },
                Godiste: { required: true, min: minimumVehicleYear, max: maximumVehicleYear, digits: true },
                Gorivo: "required",
                Kubikaza: { min: 0, number: true },
                Boja: "required",
                Kilometraza: { required: true, min: 0, number: true },
                Cijena: { required: true, min: 0.01, number: true },
                Slika: { required: form.dataset.isEdit !== "true" },
                Opis: { required: true, maxlength: 2000 }
            },
            messages: {
                Marka: { required: text.makeRequired, maxlength: text.makeMax },
                Model: { required: text.modelRequired, maxlength: text.modelMax },
                Godiste: { required: text.yearRequired, min: text.yearMin, max: text.yearMax, digits: text.yearDigits },
                Gorivo: text.fuelRequired,
                Kubikaza: { min: text.displacementPositive, number: text.displacementNumber },
                Boja: text.colorRequired,
                Kilometraza: { required: text.mileageRequired, min: text.mileagePositive, number: text.mileageNumber },
                Cijena: { required: text.priceRequired, min: text.pricePositive, number: text.priceNumber },
                Slika: { required: text.imageRequired },
                Opis: { required: text.descriptionRequired, maxlength: text.descriptionMax }
            }
        });

        $form.find('input[type="file"]').off("change.adminVehicleForm").on("change.adminVehicleForm", function () {
            const file = this.files?.[0];
            if (!file) return;
            if (!/(\.jpg|\.jpeg|\.png|\.gif|\.bmp)$/i.test(file.name)) {
                window.showAppToast(text.allowedExtensions, "error");
                $(this).val("");
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                window.showAppToast(text.imageSizeLimit, "error");
                $(this).val("");
            }
        });
    };

    const initialize = () => {
        const root = document.getElementById("vozila-list-container");
        if (!root || root.dataset.initialized === "true") return;
        root.dataset.initialized = "true";

        const text = JSON.parse(root.dataset.texts || "{}");
        const fuelLabels = JSON.parse(root.dataset.fuelLabels || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#vozila-table-body");
        const $mobileList = $("#vozila-mobile-list");
        const $searchInput = $("#vozilo-search-input");
        const $searchButton = $("#vozilo-search-button");
        const $formContainer = $("#add-vozilo-form-container");
        const $formPlaceholder = $("#add-vozilo-form-placeholder");
        const $formTitle = $("#add-edit-form-title");
        const $sortSelect = $("#vozilo-sort-select");
        const sortDefaults = { godiste: "desc", kilometraza: "asc", cijena: "desc" };
        let fuelFilter = "";
        let currentSort = "cijena";
        let currentDirection = "desc";

        const normalizeDirection = (value, sort) => ["asc", "desc"].includes(value) ? value : sortDefaults[sort] || "desc";
        const nextDirection = (sort) => sort === currentSort
            ? (currentDirection === "desc" ? "asc" : "desc")
            : sortDefaults[sort] || "desc";
        const formatCurrency = (value) => `€${Number(value || 0).toLocaleString("de-DE", { maximumFractionDigits: 0 })}`;
        const formatMileage = (value) => `${Number(value || 0).toLocaleString("de-DE")} km`;
        const fuelLabel = (value) => fuelLabels[value] || value || text.notAvailable;

        const vehicleActions = (id, name) => `<div class="table-actions">`
            + `<button class="edit-vozilo-button btn btn-sm btn-outline-primary rounded-3" data-id="${id}"><i class="bi bi-pencil-square me-1"></i>${escapeHtml(text.edit)}</button>`
            + `<button class="delete-vozilo-button btn btn-sm btn-outline-danger rounded-3" data-id="${id}" data-confirm-delete="true" `
            + `data-confirm-title="${escapeAttribute(text.deleteTitle)}" data-confirm-message="${escapeAttribute(text.deleteMessage)}" `
            + `data-confirm-item="${escapeAttribute(name)}" data-confirm-action="${escapeAttribute(text.deleteAction)}">`
            + `<i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;

        const updateSortControls = () => {
            $(".sortable-th").each(function () {
                const $link = $(this);
                const sort = $link.data("sort-key");
                const active = sort === currentSort;
                $link.toggleClass("active", active);
                $link.find("i").removeClass("bi-arrow-down-up bi-arrow-down-short bi-arrow-up-short")
                    .addClass(active ? (currentDirection === "asc" ? "bi-arrow-up-short" : "bi-arrow-down-short") : "bi-arrow-down-up");
                const params = new URLSearchParams(window.location.search);
                params.set("section", "Vozila");
                params.set("sort", sort);
                params.set("direction", nextDirection(sort));
                $searchInput.val() ? params.set("searchQuery", $searchInput.val()) : params.delete("searchQuery");
                fuelFilter ? params.set("gorivoFilter", fuelFilter) : params.delete("gorivoFilter");
                $link.attr("href", `?${params}`);
            });
            $sortSelect.val(`${currentSort}|${currentDirection}`);
        };

        const updateUrl = () => {
            const params = new URLSearchParams(window.location.search);
            params.set("section", "Vozila");
            params.set("sort", currentSort);
            params.set("direction", currentDirection);
            $searchInput.val() ? params.set("searchQuery", $searchInput.val()) : params.delete("searchQuery");
            fuelFilter ? params.set("gorivoFilter", fuelFilter) : params.delete("gorivoFilter");
            window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
            updateSortControls();
        };

        const readInitialState = () => {
            const params = new URLSearchParams(window.location.search);
            if (sortDefaults[params.get("sort")]) currentSort = params.get("sort");
            currentDirection = normalizeDirection(params.get("direction"), currentSort);
            const initialFuel = params.get("gorivoFilter");
            if (initialFuel && $(`.filter-chip[data-vehicle-fuel="${initialFuel}"]`).length) {
                fuelFilter = initialFuel;
                $(".filter-chip").removeClass("active");
                $(`.filter-chip[data-vehicle-fuel="${initialFuel}"]`).addClass("active");
            }
            if (!$searchInput.val()) $searchInput.val(params.get("searchQuery") || root.dataset.initialSearch || "");
            updateSortControls();
        };

        const renderVehicles = (vehicles) => {
            $tableBody.empty();
            $mobileList.empty();
            if (!vehicles?.length) {
                const empty = `<div class="empty-state"><i class="bi bi-car-front"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="6">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            $.each(vehicles, (_, vehicle) => {
                const id = vehicle.voziloID;
                const rawName = vehicle.naziv || text.unknownVehicle;
                const name = escapeHtml(rawName);
                const year = escapeHtml(vehicle.godiste || text.notAvailable);
                const fuel = escapeHtml(fuelLabel(vehicle.gorivo));
                const mileage = formatMileage(vehicle.kilometraza);
                const price = formatCurrency(vehicle.cijena);
                const buttons = vehicleActions(id, rawName);
                $tableBody.append(`<tr><td><div class="vehicle-name-cell" title="${escapeAttribute(rawName)}"><strong>${name}</strong><span>ID #${id}</span></div></td>`
                    + `<td>${year}</td><td><span class="admin-badge">${fuel}</span></td><td>${mileage}</td>`
                    + `<td><strong class="price-cell">${price}</strong></td><td>${buttons}</td></tr>`);
                $mobileList.append(`<article class="admin-vehicle-card"><div class="d-flex justify-content-between gap-3"><div><span class="admin-badge">${fuel}</span>`
                    + `<h3>${name}</h3></div><strong class="price-cell">${price}</strong></div><div class="mobile-specs">`
                    + `<div><span>${escapeHtml(text.year)}</span><strong>${year}</strong></div><div><span>${escapeHtml(text.mileage)}</span><strong>${mileage}</strong></div>`
                    + `</div><div class="mobile-actions">${buttons}</div></article>`);
            });
        };

        const loadVehicles = () => {
            $tableBody.html(`<tr><td colspan="6">${escapeHtml(text.loading)}</td></tr>`);
            $mobileList.html(`<div class="empty-state"><i class="bi bi-hourglass-split"></i><p>${escapeHtml(text.loading)}</p></div>`);
            window.appApi.get(root.dataset.listUrl, {
                searchQuery: $searchInput.val(), page: 1, sort: currentSort, direction: currentDirection, gorivoFilter: fuelFilter
            }).done((data) => renderVehicles(data?.vozila || [])).fail((xhr) => {
                const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                const error = `<div class="alert alert-danger mb-0">${escapeHtml(text.loadError)} ${message}</div>`;
                $tableBody.html(`<tr><td colspan="6">${error}</td></tr>`);
                $mobileList.html(error);
            });
        };

        const loadForm = (url, params, title, errorText) => {
            $formTitle.text(title);
            window.appApi.request({ url, type: "GET", data: params, dataType: "html" }).done((html) => {
                $formPlaceholder.html(html);
                $formContainer.show();
                window.adminCore.parseUnobtrusiveValidation($formPlaceholder.find("form"));
                initializeVehicleForm();
            }).fail(() => {
                $formPlaceholder.html(`<div class="alert alert-danger mb-0">${escapeHtml(errorText)}</div>`);
                $formContainer.show();
            });
        };

        $searchButton.off("click.adminVehicles").on("click.adminVehicles", () => { updateUrl(); loadVehicles(); });
        $searchInput.off("keydown.adminVehicles").on("keydown.adminVehicles", (event) => {
            if (event.key === "Enter") { event.preventDefault(); updateUrl(); loadVehicles(); }
        });
        $(document).off("click.adminVehiclesFilter", ".filter-chip").on("click.adminVehiclesFilter", ".filter-chip", function () {
            fuelFilter = $(this).data("vehicle-fuel") || "";
            $(".filter-chip").removeClass("active");
            $(this).addClass("active");
            updateUrl();
            loadVehicles();
        });
        $(document).off("click.adminVehiclesSort", ".sortable-th").on("click.adminVehiclesSort", ".sortable-th", function (event) {
            event.preventDefault();
            const sort = $(this).data("sort-key");
            if (!sortDefaults[sort]) return;
            currentDirection = nextDirection(sort);
            currentSort = sort;
            updateUrl();
            loadVehicles();
        });
        $sortSelect.off("change.adminVehicles").on("change.adminVehicles", function () {
            const [sort, direction] = ($(this).val() || "cijena|desc").split("|");
            currentSort = sortDefaults[sort] ? sort : "cijena";
            currentDirection = normalizeDirection(direction, currentSort);
            updateUrl();
            loadVehicles();
        });
        $("#add-vozilo-button").off("click.adminVehicles").on("click.adminVehicles", () => loadForm(root.dataset.addFormUrl, {}, text.addVehicle, text.addFormLoadError));
        $(document).off("click.adminVehiclesEdit", ".edit-vozilo-button").on("click.adminVehiclesEdit", ".edit-vozilo-button", function () {
            loadForm(root.dataset.editFormUrl, { id: $(this).data("id") }, text.editVehicle, text.editFormLoadError);
        });
        $(document).off("click.adminVehiclesCancel", "#cancel-add-vozilo").on("click.adminVehiclesCancel", "#cancel-add-vozilo", () => {
            $formContainer.hide();
            $formPlaceholder.empty();
        });
        $(document).off("submit.adminVehiclesSave", "#add-edit-vozilo-form").on("submit.adminVehiclesSave", "#add-edit-vozilo-form", function (event) {
            event.preventDefault();
            const $form = $(this);
            if (!$form.valid()) return;
            window.appApi.request({ url: $form.attr("action"), type: $form.attr("method"), data: new FormData(this), processData: false, contentType: false })
                .done(() => {
                    $formContainer.hide();
                    $formPlaceholder.empty();
                    loadVehicles();
                    window.showAppToast(text.saveSuccess, "success");
                }).fail((xhr) => window.showAppToast(xhr.status === 400 && xhr.responseJSON ? text.saveValidationError : text.saveImageRequiredError, "error"));
        });
        $(document).off("app:delete-confirmed.adminVehicles", ".delete-vozilo-button")
            .on("app:delete-confirmed.adminVehicles", ".delete-vozilo-button", function () {
                window.appApi.post(root.dataset.deleteUrl, { id: $(this).data("id") })
                    .done((response) => { window.showAppToast(response.successMessage || text.deleteSuccess, "success"); loadVehicles(); })
                    .fail(() => window.showAppToast(text.deleteError, "error"));
            });

        readInitialState();
        loadVehicles();
    };

    $(document).on("admin:section-loaded.adminVehicles", (_, sectionName) => {
        if (sectionName === "Vozila") initialize();
    });
})();
