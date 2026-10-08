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
        const currentImagePreview = document.getElementById("current-vehicle-image-preview");
        const selectedImagePreview = document.getElementById("selected-vehicle-image-preview");
        const selectedImage = document.getElementById("selected-vehicle-image");
        const mediaLayout = document.getElementById("admin-vehicle-media-layout");
        const imageColumn = document.getElementById("admin-vehicle-image-column");
        const colorSelect = form.elements.Boja;
        const fuelSelect = form.elements.Gorivo;
        const fuelPicker = form.querySelector("[data-option-picker]");
        const colorPicker = form.querySelector("[data-color-picker]");
        const displacementField = document.getElementById("vehicle-displacement-field");
        const displacementInput = form.elements.Kubikaza;
        const submitButton = form.querySelector('button[type="submit"]');
        const maximumSourceBytes = 10 * 1024 * 1024;
        const maximumProcessedBytes = 2 * 1024 * 1024;
        const maximumDetailWidth = 1600;
        const maximumDetailHeight = 1000;
        const webpQuality = 0.83;
        let selectedImageUrl = "";
        let imageSelectionVersion = 0;

        const resetSelectedImagePreview = () => {
            if (selectedImageUrl) {
                URL.revokeObjectURL(selectedImageUrl);
                selectedImageUrl = "";
            }
            selectedImage?.removeAttribute("src");
            selectedImagePreview?.classList.add("d-none");
            selectedImagePreview?.removeAttribute("data-image-src");
            currentImagePreview?.classList.remove("d-none");
            if (currentImagePreview) {
                imageColumn?.classList.remove("d-none");
                mediaLayout?.classList.add("has-image");
            } else {
                imageColumn?.classList.add("d-none");
                mediaLayout?.classList.remove("has-image");
            }
        };

        const setImageProcessingState = (isProcessing) => {
            form.setAttribute("aria-busy", isProcessing ? "true" : "false");
            if (submitButton) submitButton.disabled = isProcessing;
        };

        const decodeImage = async (file) => {
            if (typeof window.createImageBitmap === "function") {
                try {
                    const bitmap = await window.createImageBitmap(file, { imageOrientation: "from-image" });
                    return {
                        source: bitmap,
                        width: bitmap.width,
                        height: bitmap.height,
                        dispose: () => bitmap.close()
                    };
                } catch {
                    // The image element fallback covers browsers with partial createImageBitmap support.
                }
            }

            const sourceUrl = URL.createObjectURL(file);
            try {
                const image = new Image();
                image.decoding = "async";
                image.src = sourceUrl;
                await image.decode();
                return {
                    source: image,
                    width: image.naturalWidth,
                    height: image.naturalHeight,
                    dispose: () => URL.revokeObjectURL(sourceUrl)
                };
            } catch (error) {
                URL.revokeObjectURL(sourceUrl);
                throw error;
            }
        };

        const canvasToWebp = (canvas) => new Promise((resolve, reject) => {
            canvas.toBlob((blob) => {
                if (!blob || blob.type !== "image/webp") {
                    reject(new Error("WebP encoding is not supported."));
                    return;
                }
                resolve(blob);
            }, "image/webp", webpQuality);
        });

        const optimizeImage = async (file) => {
            const decoded = await decodeImage(file);
            try {
                if (!decoded.width || !decoded.height) {
                    throw new Error("The image has invalid dimensions.");
                }

                const scale = Math.min(
                    1,
                    maximumDetailWidth / decoded.width,
                    maximumDetailHeight / decoded.height);
                const width = Math.max(1, Math.round(decoded.width * scale));
                const height = Math.max(1, Math.round(decoded.height * scale));
                const canvas = document.createElement("canvas");
                canvas.width = width;
                canvas.height = height;
                const context = canvas.getContext("2d", { alpha: false });
                if (!context) throw new Error("Canvas is unavailable.");

                context.imageSmoothingEnabled = true;
                context.imageSmoothingQuality = "high";
                context.fillStyle = "#ffffff";
                context.fillRect(0, 0, width, height);
                context.drawImage(decoded.source, 0, 0, width, height);
                const blob = await canvasToWebp(canvas);
                if (blob.size > maximumProcessedBytes) {
                    throw new Error("The optimized image is too large.");
                }

                const baseName = file.name.replace(/\.[^.]+$/, "") || "vehicle";
                return new File([blob], `${baseName}.webp`, {
                    type: "image/webp",
                    lastModified: Date.now()
                });
            } finally {
                decoded.dispose();
            }
        };

        const showSelectedImagePreview = (file) => {
            resetSelectedImagePreview();
            if (!selectedImagePreview || !selectedImage) return;

            selectedImageUrl = URL.createObjectURL(file);
            selectedImage.src = selectedImageUrl;
            selectedImage.alt = file.name;
            selectedImagePreview.dataset.imageSrc = selectedImageUrl;
            selectedImagePreview.classList.remove("d-none");
            currentImagePreview?.classList.add("d-none");
            imageColumn?.classList.remove("d-none");
            mediaLayout?.classList.add("has-image");
        };

        $form.removeData("validator").removeData("unobtrusiveValidation");
        $form.validate({
            ignore: ":hidden:not(#Boja):not(#Gorivo)",
            errorElement: "span",
            errorClass: "text-danger",
            highlight: (element) => {
                $(element).addClass("is-invalid").removeClass("is-valid");
                if (element.name === "Boja" || element.name === "Gorivo") {
                    $(element.name === "Gorivo" ? fuelPicker : colorPicker)
                        .find(".admin-option-select-toggle, .admin-color-select-toggle")
                        .addClass("is-invalid").removeClass("is-valid");
                }
            },
            unhighlight: (element) => {
                $(element).addClass("is-valid").removeClass("is-invalid");
                if (element.name === "Boja" || element.name === "Gorivo") {
                    $(element.name === "Gorivo" ? fuelPicker : colorPicker)
                        .find(".admin-option-select-toggle, .admin-color-select-toggle")
                        .addClass("is-valid").removeClass("is-invalid");
                }
            },
            errorPlacement: (error, element) => {
                if (element.attr("name") === "Boja" || element.attr("name") === "Gorivo") {
                    error.insertAfter(element.attr("name") === "Gorivo" ? fuelPicker : colorPicker);
                    return;
                }
                error.insertAfter(element);
            },
            rules: {
                Marka: { required: true, maxlength: 100 },
                Model: { required: true, maxlength: 100 },
                Godiste: { required: true, min: minimumVehicleYear, max: maximumVehicleYear, digits: true },
                Gorivo: "required",
                Kubikaza: { min: 0.1, number: true },
                Boja: "required",
                Kilometraza: { required: true, min: 0, step: 1, digits: true },
                Cijena: { required: true, min: 1, step: 1, digits: true },
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
                Kilometraza: { required: text.mileageRequired, min: text.mileagePositive, step: text.mileageDigits, digits: text.mileageDigits },
                Cijena: { required: text.priceRequired, min: text.pricePositive, step: text.priceDigits, digits: text.priceDigits },
                Slika: { required: text.imageRequired },
                Opis: { required: text.descriptionRequired, maxlength: text.descriptionMax }
            }
        });

        window.vehicleColorPicker?.initialize(form);
        const syncDisplacement = () => {
            const isElectric = fuelSelect?.value === "Elektro";
            $(displacementField).toggleClass("d-none", isElectric);
            if (!displacementInput) return;
            displacementInput.disabled = isElectric;
            if (isElectric) {
                $(displacementInput).val("");
                window.adminCore.clearFieldValidationError(form, displacementInput.name);
            }
        };
        $(colorSelect).off("change.adminVehicleColorValidation").on("change.adminVehicleColorValidation", function () {
            window.adminCore.clearFieldValidationError(form, colorSelect.name);
            $form.validate().element(colorSelect);
        });
        $(fuelSelect).off("change.adminVehicleFuelValidation").on("change.adminVehicleFuelValidation", function () {
            window.adminCore.clearFieldValidationError(form, fuelSelect.name);
            $form.validate().element(fuelSelect);
            syncDisplacement();
        });
        syncDisplacement();

        $form.find('input[type="file"]').off("change.adminVehicleForm").on("change.adminVehicleForm", async function () {
            const selectionVersion = ++imageSelectionVersion;
            const file = this.files?.[0];
            form.vehicleProcessedImage = null;
            if (!file) {
                resetSelectedImagePreview();
                return;
            }
            window.adminCore.clearFieldValidationError(form, this.name);
            if (!/(\.jpg|\.jpeg|\.png|\.webp)$/i.test(file.name)) {
                window.adminCore.applyValidationErrors(form, { [this.name]: [text.allowedExtensions] });
                $(this).val("");
                resetSelectedImagePreview();
                return;
            }
            if (file.size > maximumSourceBytes) {
                window.adminCore.applyValidationErrors(form, { [this.name]: [text.imageSourceSizeLimit] });
                $(this).val("");
                resetSelectedImagePreview();
                return;
            }

            setImageProcessingState(true);
            const processingPromise = optimizeImage(file);
            form.vehicleImageProcessingPromise = processingPromise;
            try {
                const processedImage = await processingPromise;
                if (selectionVersion !== imageSelectionVersion) return;
                form.vehicleProcessedImage = processedImage;
                showSelectedImagePreview(processedImage);
            } catch {
                if (selectionVersion !== imageSelectionVersion) return;
                window.adminCore.applyValidationErrors(form, { [this.name]: [text.imageProcessingError] });
                $(this).val("");
                resetSelectedImagePreview();
            } finally {
                if (selectionVersion === imageSelectionVersion) {
                    setImageProcessingState(false);
                }
            }
        });

        form.addEventListener("reset", () => {
            imageSelectionVersion++;
            form.vehicleProcessedImage = null;
            form.vehicleImageProcessingPromise = null;
            setImageProcessingState(false);
            resetSelectedImagePreview();
        }, { once: true });
    };

    const initialize = () => {
        const root = document.getElementById("vozila-list-container");
        if (!root || root.dataset.initialized === "true") return;
        root.dataset.initialized = "true";

        const text = JSON.parse(root.dataset.texts || "{}");
        const fuelLabels = JSON.parse(root.dataset.fuelLabels || "{}");
        const fuelClasses = JSON.parse(root.dataset.fuelClasses || "{}");
        const colorLabels = JSON.parse(root.dataset.colorLabels || "{}");
        const colorClasses = JSON.parse(root.dataset.colorClasses || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#vozila-table-body");
        const $mobileList = $("#vozila-mobile-list");
        const $searchInput = $("#vozilo-search-input");
        const $searchButton = $("#vozilo-search-button");
        const $statusFilter = $("#vehicle-status-filter");
        const $fuelFilter = $("#vehicle-fuel-filter");
        const $colorFilter = $("#vehicle-color-filter");
        const $listView = $("#vozila-list-view");
        const $formContainer = $("#add-vozilo-form-container");
        const $formPlaceholder = $("#add-vozilo-form-placeholder");
        const $formTitle = $("#add-edit-form-title");
        const $sortSelect = $("#vozilo-sort-select");
        const panelRoot = document.getElementById("admin-panel-root");
        const externalEditId = Number.parseInt(panelRoot?.dataset.editVehicleId || "", 10);
        const externalReturnUrl = Number.isInteger(externalEditId) && externalEditId > 0
            ? panelRoot?.dataset.returnUrl || ""
            : "";
        const sortDefaults = { godiste: "desc", kilometraza: "asc", cijena: "desc" };
        const statusFilters = new Set(["available", "sold", "all"]);
        let statusFilter = "available";
        let fuelFilter = "";
        let colorFilter = "";
        let currentSort = "cijena";
        let currentDirection = "desc";
        let listScrollPosition = 0;
        let formReturnSection = "";
        let activeListRequest = null;
        const pager = window.adminLazyList.create({
            element: root.querySelector("[data-admin-lazy-controls]"),
            loadMoreText: text.loadMore,
            onLoadMore: (offset) => loadVehicles(offset, true)
        });

        const normalizeDirection = (value, sort) => ["asc", "desc"].includes(value) ? value : sortDefaults[sort] || "desc";
        const nextDirection = (sort) => sort === currentSort
            ? (currentDirection === "desc" ? "asc" : "desc")
            : sortDefaults[sort] || "desc";
        const formatCurrency = (value) => `€${Number(value || 0).toLocaleString("de-DE", { maximumFractionDigits: 0 })}`;
        const formatMileage = (value) => `${Number(value || 0).toLocaleString("de-DE")} km`;
        const fuelLabel = (value) => fuelLabels[value] || value || text.notAvailable;
        const fuelChip = (value) => `<span class="fuel-chip ${escapeAttribute(fuelClasses[value] || "is-default")}">${escapeHtml(fuelLabel(value))}</span>`;
        const colorLabel = (value) => colorLabels[value] || value || text.notAvailable;
        const colorDisplay = (value) => `<span class="admin-vehicle-color" title="${escapeAttribute(colorLabel(value))}">`
            + `<span class="vehicle-color-swatch ${escapeAttribute(colorClasses[value] || "is-other")}" aria-hidden="true"></span>`
            + `<span>${escapeHtml(colorLabel(value))}</span></span>`;
        const statusDisplay = (isAvailable) => `<span class="admin-vehicle-status ${isAvailable ? "is-available" : "is-sold"}">`
            + `<i class="bi ${isAvailable ? "bi-check-circle-fill" : "bi-lock-fill"}" aria-hidden="true"></i>`
            + `${escapeHtml(isAvailable ? text.available : text.sold)}</span>`;

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
                statusFilter === "available" ? params.delete("statusFilter") : params.set("statusFilter", statusFilter);
                fuelFilter ? params.set("gorivoFilter", fuelFilter) : params.delete("gorivoFilter");
                colorFilter ? params.set("bojaFilter", colorFilter) : params.delete("bojaFilter");
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
            statusFilter === "available" ? params.delete("statusFilter") : params.set("statusFilter", statusFilter);
            fuelFilter ? params.set("gorivoFilter", fuelFilter) : params.delete("gorivoFilter");
            colorFilter ? params.set("bojaFilter", colorFilter) : params.delete("bojaFilter");
            window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
            updateSortControls();
        };

        const readInitialState = () => {
            const params = new URLSearchParams(window.location.search);
            if (sortDefaults[params.get("sort")]) currentSort = params.get("sort");
            currentDirection = normalizeDirection(params.get("direction"), currentSort);
            const initialStatus = params.get("statusFilter");
            if (statusFilters.has(initialStatus)) statusFilter = initialStatus;
            const initialFuel = params.get("gorivoFilter");
            if (initialFuel && $fuelFilter.find("option").filter((_, option) => option.value === initialFuel).length) fuelFilter = initialFuel;
            const initialColor = params.get("bojaFilter");
            if (initialColor && $colorFilter.find("option").filter((_, option) => option.value === initialColor).length) colorFilter = initialColor;
            $statusFilter.val(statusFilter);
            $fuelFilter.val(fuelFilter);
            $colorFilter.val(colorFilter);
            window.vehicleColorPicker?.sync($colorFilter.closest("[data-color-picker]")[0]);
            if (!$searchInput.val()) $searchInput.val(params.get("searchQuery") || root.dataset.initialSearch || "");
            updateSortControls();
        };

        const renderVehicles = (vehicles, append = false) => {
            if (!append) {
                $tableBody.empty();
                $mobileList.empty();
            }
            if (!vehicles?.length) {
                if (append) return;
                const empty = `<div class="empty-state"><i class="bi bi-car-front"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="7">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            $.each(vehicles, (_, vehicle) => {
                const id = vehicle.voziloID;
                const rawName = vehicle.naziv || text.unknownVehicle;
                const name = escapeHtml(rawName);
                const year = escapeHtml(vehicle.godiste || text.notAvailable);
                const fuel = fuelChip(vehicle.gorivo);
                const color = colorDisplay(vehicle.boja);
                const status = statusDisplay(vehicle.dostupnoZaKupovinu === true);
                const mileage = formatMileage(vehicle.kilometraza);
                const price = formatCurrency(vehicle.cijena);
                const buttons = vehicleActions(id, rawName);
                $tableBody.append(`<tr data-admin-item-id="${id}"><td><div class="vehicle-name-cell" title="${escapeAttribute(rawName)}"><strong>${name}</strong>`
                    + `<div class="vehicle-name-meta"><span>ID #${id}</span>${status}</div></div></td>`
                    + `<td>${year}</td><td>${fuel}</td><td>${color}</td><td>${mileage}</td>`
                    + `<td><strong class="price-cell">${price}</strong></td><td>${buttons}</td></tr>`);
                $mobileList.append(`<article class="admin-vehicle-card" data-admin-item-id="${id}"><div class="d-flex justify-content-between gap-3"><div><div class="admin-vehicle-card-tags">${fuel}${color}${status}</div>`
                    + `<h3>${name}</h3></div><strong class="price-cell">${price}</strong></div><div class="mobile-specs">`
                    + `<div><span>${escapeHtml(text.year)}</span><strong>${year}</strong></div><div><span>${escapeHtml(text.mileage)}</span><strong>${mileage}</strong></div>`
                    + `</div><div class="mobile-actions">${buttons}</div></article>`);
            });
        };

        const loadVehicles = (offset = 0, append = false) => {
            activeListRequest?.abort();
            if (append) {
                pager.setLoading(true);
            } else {
                pager.reset();
                window.adminCore.setLoading(root, true);
            }
            const request = window.appApi.get(root.dataset.listUrl, {
                searchQuery: $searchInput.val(), offset, sort: currentSort, direction: currentDirection,
                gorivoFilter: fuelFilter, bojaFilter: colorFilter, statusFilter
            }).done((data) => {
                const vehicles = data?.vozila || [];
                renderVehicles(vehicles, append);
                pager.update(data, vehicles.length, append);
            }).fail((xhr, statusText) => {
                if (statusText === "abort") return;
                if (append) {
                    window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error");
                    return;
                }
                const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                const error = `<div class="alert alert-danger mb-0">${message}</div>`;
                $tableBody.html(`<tr><td colspan="7">${error}</td></tr>`);
                $mobileList.html(error);
            }).always(() => {
                if (activeListRequest !== request) return;
                activeListRequest = null;
                pager.setLoading(false);
                if (!append) window.adminCore.setLoading(root, false);
            });
            activeListRequest = request;
        };

        const loadForm = (url, params, title, errorText) => {
            if ($listView.is(":visible")) {
                listScrollPosition = window.scrollY;
            }

            $formTitle.text(title);
            $listView.hide();
            $formContainer.show();
            $formPlaceholder.html(`<div class="empty-state"><i class="bi bi-hourglass-split"></i><p>${escapeHtml(text.loading)}</p></div>`);
            window.adminCore.scrollToElement($formContainer[0]);

            window.appApi.request({ url, type: "GET", data: params, dataType: "html" }).done((html) => {
                $formPlaceholder.html(html);
                window.adminCore.parseUnobtrusiveValidation($formPlaceholder.find("form"));
                initializeVehicleForm();
            }).fail(() => {
                $formPlaceholder.html(`<div class="alert alert-danger mb-0">${escapeHtml(errorText)}</div>`);
            });
        };

        const showListView = (reload = false) => {
            $formContainer.hide();
            $formPlaceholder.empty();
            $listView.show();
            if (reload) loadVehicles();

            window.requestAnimationFrame(() => window.scrollTo({ top: listScrollPosition, behavior: "auto" }));
        };

        $searchButton.off("click.adminVehicles").on("click.adminVehicles", () => { updateUrl(); loadVehicles(); });
        $searchInput.off("keydown.adminVehicles").on("keydown.adminVehicles", (event) => {
            if (event.key === "Enter") { event.preventDefault(); updateUrl(); loadVehicles(); }
        });
        $statusFilter.off("change.adminVehicles").on("change.adminVehicles", function () {
            statusFilter = statusFilters.has(this.value) ? this.value : "available";
            updateUrl();
            loadVehicles();
        });
        $fuelFilter.off("change.adminVehicles").on("change.adminVehicles", function () {
            fuelFilter = this.value || "";
            updateUrl();
            loadVehicles();
        });
        $colorFilter.off("change.adminVehicles").on("change.adminVehicles", function () {
            colorFilter = this.value || "";
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
        $("#add-vozilo-button").off("click.adminVehicles").on("click.adminVehicles", function () {
            formReturnSection = $(this).data("return-section") || "";
            loadForm(root.dataset.addFormUrl, {}, text.addVehicle, text.addFormLoadError);
        });
        $(document).off("click.adminVehiclesEdit", ".edit-vozilo-button").on("click.adminVehiclesEdit", ".edit-vozilo-button", function () {
            formReturnSection = "";
            loadForm(root.dataset.editFormUrl, { id: $(this).data("id") }, text.editVehicle, text.editFormLoadError);
        });
        $(document).off("click.adminVehiclesCancel", "#cancel-add-vozilo, #back-to-vozila-list").on("click.adminVehiclesCancel", "#cancel-add-vozilo, #back-to-vozila-list", () => {
            if (formReturnSection) {
                window.loadSection(formReturnSection, {}, true);
                return;
            }
            if (externalReturnUrl) {
                window.location.assign(externalReturnUrl);
                return;
            }

            showListView();
        });
        $(document).off("submit.adminVehiclesSave", "#add-edit-vozilo-form").on("submit.adminVehiclesSave", "#add-edit-vozilo-form", async function (event) {
            event.preventDefault();
            const form = this;
            const $form = $(this);
            window.adminCore.clearValidationErrors(form);

            const imageInput = form.elements.Slika;
            if (imageInput?.files?.length) {
                try {
                    await form.vehicleImageProcessingPromise;
                } catch {
                    window.adminCore.applyValidationErrors(form, { [imageInput.name]: [text.imageProcessingError] });
                    return;
                }

                if (!form.vehicleProcessedImage) {
                    window.adminCore.applyValidationErrors(form, { [imageInput.name]: [text.imageProcessingError] });
                    return;
                }
            }

            if (!$form.valid()) return;
            const formData = new FormData(form);
            if (form.vehicleProcessedImage) {
                formData.set(imageInput.name, form.vehicleProcessedImage, form.vehicleProcessedImage.name);
            }
            window.appApi.request({ url: $form.attr("action"), type: $form.attr("method"), data: formData, processData: false, contentType: false })
                .done((response) => {
                    const successMessage = response.successMessage || text.saveSuccess;
                    if (externalReturnUrl) {
                        window.queueAppToast(successMessage, "success");
                        window.location.assign(externalReturnUrl);
                        return;
                    }

                    window.showAppToast(successMessage, "success");
                    formReturnSection = "";
                    showListView(true);
                }).fail((xhr) => {
                    if (xhr.status === 400 && xhr.responseJSON?.errors) {
                        const unmatched = window.adminCore.applyValidationErrors(form, xhr.responseJSON.errors);
                        if (unmatched.length) {
                            window.showAppToast(unmatched.join(" "), "error");
                        }
                        return;
                    }

                    window.showAppToast(window.adminCore.errorMessage(xhr, text.saveImageRequiredError), "error");
                });
        });
        $(document).off("app:delete-confirmed.adminVehicles", ".delete-vozilo-button")
            .on("app:delete-confirmed.adminVehicles", ".delete-vozilo-button", function () {
                const id = $(this).data("id");
                window.appApi.post(root.dataset.deleteUrl, { id })
                    .done((response) => {
                        window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                        window.adminCore.removeRenderedItem(root, id, () => renderVehicles([]));
                        pager.removeItem();
                    })
                    .fail(() => window.showAppToast(text.deleteError, "error"));
            });

        window.vehicleColorPicker?.initialize(root);
        readInitialState();
        loadVehicles();

        if (Number.isInteger(externalEditId) && externalEditId > 0) {
            if (panelRoot) {
                panelRoot.dataset.editVehicleId = "";
                panelRoot.dataset.returnUrl = "";
            }
            loadForm(root.dataset.editFormUrl, { id: externalEditId }, text.editVehicle, text.editFormLoadError);
        }
    };

    $(document).on("admin:section-loaded.adminVehicles", (_, sectionName) => {
        if (sectionName === "Vozila") initialize();
    });
})();
