(() => {
    const mobileQuery = window.matchMedia("(max-width: 767.98px)");

    const initialize = () => {
        const root = document.getElementById("admin-panel-root");
        if (!root || root.dataset.mobileAdminInitialized === "true") return;
        root.dataset.mobileAdminInitialized = "true";

        const sidebar = root.querySelector(".admin-sidebar");
        const menu = root.querySelector(".admin-menu");
        const menuToggle = root.querySelector(".admin-mobile-section-toggle");
        const menuBackdrop = root.querySelector(".admin-mobile-menu-backdrop");
        const menuClose = root.querySelector(".admin-mobile-menu-close");
        const currentSection = root.querySelector(".admin-mobile-section-current");
        const content = root.querySelector("#admin-section-content");
        const closeText = root.dataset.textClose || "Close";
        const filtersText = root.dataset.textFilters || "Filters";
        const sortText = root.dataset.textSort || "Sort";
        const statusText = root.dataset.textStatus || "Status";
        const fuelText = root.dataset.textFuel || "Fuel";
        const colorText = root.dataset.textColor || "Color";
        const backText = root.dataset.textBack || "Back";
        const removeText = root.dataset.textRemove || "Remove";
        let activeSheet = null;

        const toolsBackdrop = document.createElement("button");
        toolsBackdrop.type = "button";
        toolsBackdrop.className = "admin-mobile-tools-backdrop";
        toolsBackdrop.setAttribute("aria-label", closeText);
        toolsBackdrop.tabIndex = -1;
        root.append(toolsBackdrop);

        const syncBodyLock = () => {
            const overlayOpen = sidebar?.classList.contains("is-mobile-menu-open") || Boolean(activeSheet);
            document.body.classList.toggle("admin-mobile-overlay-open", mobileQuery.matches && overlayOpen);
        };

        const updateCurrentSection = (link) => {
            if (!link || !currentSection) return;
            const icon = link.querySelector("i")?.cloneNode(true);
            const label = link.textContent.trim();
            currentSection.replaceChildren();
            if (icon) {
                icon.setAttribute("aria-hidden", "true");
                currentSection.append(icon);
            }
            const text = document.createElement("span");
            text.textContent = label;
            currentSection.append(text);
        };

        const closeMenu = (restoreFocus = false) => {
            if (!sidebar?.classList.contains("is-mobile-menu-open")) return;
            sidebar.classList.remove("is-mobile-menu-open");
            menuToggle?.setAttribute("aria-expanded", "false");
            syncBodyLock();
            if (restoreFocus) menuToggle?.focus();
        };

        const openMenu = () => {
            if (!mobileQuery.matches || !sidebar) return;
            closeSheet();
            sidebar.classList.add("is-mobile-menu-open");
            menuToggle?.setAttribute("aria-expanded", "true");
            syncBodyLock();
            window.requestAnimationFrame(() => menuClose?.focus());
        };

        const closeSheetDrilldown = (element, restoreFocus = false) => {
            const drilldown = element?.querySelector(".admin-mobile-color-drilldown.is-open");
            if (!drilldown) return false;
            element.classList.remove("is-drilldown-open");
            drilldown.classList.remove("is-open");
            drilldown.setAttribute("aria-hidden", "true");
            if (restoreFocus) element.querySelector(".admin-mobile-color-trigger")?.focus();
            return true;
        };

        const closeSheet = (restoreFocus = false) => {
            if (!activeSheet) return;
            closeMobileSelects();
            const { element, trigger } = activeSheet;
            closeSheetDrilldown(element);
            element.classList.remove("is-open");
            element.setAttribute("aria-hidden", "true");
            toolsBackdrop.classList.remove("is-open");
            activeSheet = null;
            syncBodyLock();
            if (restoreFocus) trigger?.focus();
        };

        const openSheet = (element, trigger) => {
            if (!mobileQuery.matches || !element) return;
            closeMenu();
            if (activeSheet?.element === element) {
                closeSheet(true);
                return;
            }
            closeSheet();
            activeSheet = { element, trigger };
            element.classList.add("is-open");
            element.setAttribute("aria-hidden", "false");
            toolsBackdrop.classList.add("is-open");
            syncBodyLock();
            window.requestAnimationFrame(() => element.querySelector(".admin-mobile-sheet-close")?.focus());
        };

        const addSwipeToClose = (element, close) => {
            if (!element || element.dataset.mobileSwipeInitialized === "true") return;
            element.dataset.mobileSwipeInitialized = "true";
            let startX = 0;
            let startY = 0;

            element.addEventListener("touchstart", (event) => {
                const touch = event.changedTouches[0];
                startX = touch.clientX;
                startY = touch.clientY;
            }, { passive: true });

            element.addEventListener("touchend", (event) => {
                const touch = event.changedTouches[0];
                const deltaX = touch.clientX - startX;
                const deltaY = touch.clientY - startY;
                if (deltaY > 72 && Math.abs(deltaY) > Math.abs(deltaX) * 1.25) close();
            }, { passive: true });
        };

        const prepareSheet = (element, title, includeDoneButton = true) => {
            if (!element || element.dataset.mobileSheetInitialized === "true") return;
            element.dataset.mobileSheetInitialized = "true";
            element.classList.add("admin-mobile-sheet-panel");
            element.setAttribute("aria-hidden", mobileQuery.matches ? "true" : "false");

            const header = document.createElement("div");
            header.className = "admin-mobile-sheet-header";
            header.innerHTML = `<strong></strong><button type="button" class="admin-mobile-sheet-close" aria-label="${escapeAttribute(closeText)}"><i class="bi bi-x-lg" aria-hidden="true"></i></button>`;
            header.querySelector("strong").textContent = title;
            header.querySelector(".admin-mobile-sheet-close").addEventListener("click", () => closeSheet(true));
            element.prepend(header);

            if (includeDoneButton) {
                const footer = document.createElement("div");
                footer.className = "admin-mobile-sheet-footer";
                const done = document.createElement("button");
                done.type = "button";
                done.className = "admin-mobile-sheet-done";
                done.textContent = closeText;
                done.addEventListener("click", () => closeSheet(true));
                footer.append(done);
                element.append(footer);
            }

            addSwipeToClose(element, () => closeSheet(true));
        };

        const escapeAttribute = (value) => String(value || "")
            .replaceAll("&", "&amp;")
            .replaceAll('"', "&quot;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;");

        const createToolButton = (label, iconClass, onClick) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "admin-mobile-tool-button";
            button.innerHTML = `<i class="bi ${iconClass}" aria-hidden="true"></i><span></span>`;
            button.querySelector("span").textContent = label;
            button.addEventListener("click", onClick);
            return button;
        };

        const createTools = () => {
            const tools = document.createElement("div");
            tools.className = "admin-mobile-tools";
            return tools;
        };

        const selectedOptionText = (select) => select?.selectedOptions?.[0]?.textContent?.trim() || "";

        const closeMobileSelects = (except = null) => {
            root.querySelectorAll(".admin-mobile-select-control.is-open").forEach((control) => {
                if (control === except) return;
                control.classList.remove("is-open");
                control.querySelector(".admin-mobile-select-toggle")?.setAttribute("aria-expanded", "false");
                const options = control.querySelector(".admin-mobile-select-options");
                if (options) options.hidden = true;
            });
        };

        const syncMobileSelectMode = (select) => {
            if (!select?.classList.contains("admin-mobile-native-select")) return;
            if (mobileQuery.matches) {
                select.setAttribute("aria-hidden", "true");
                select.tabIndex = -1;
                return;
            }
            select.removeAttribute("aria-hidden");
            select.removeAttribute("tabindex");
        };

        const enhanceMobileSelects = (scope = content) => {
            if (!scope) return;

            scope.querySelectorAll("select.form-select:not([data-option-select]):not([data-color-select]):not(#vehicle-status-filter):not(#vehicle-fuel-filter):not(#vozilo-sort-select)").forEach((select) => {
                if (select.dataset.mobileSelectInitialized === "true") return;
                select.dataset.mobileSelectInitialized = "true";
                select.classList.add("admin-mobile-native-select");

                const control = document.createElement("div");
                control.className = "admin-mobile-select-control";

                const toggle = document.createElement("button");
                toggle.type = "button";
                toggle.className = "admin-mobile-select-toggle";
                toggle.setAttribute("aria-haspopup", "listbox");
                toggle.setAttribute("aria-expanded", "false");
                toggle.innerHTML = '<span data-mobile-select-label></span><i class="bi bi-chevron-down" aria-hidden="true"></i>';

                const options = document.createElement("div");
                options.className = "admin-mobile-select-options";
                options.setAttribute("role", "listbox");
                options.hidden = true;

                const sync = () => {
                    toggle.querySelector("[data-mobile-select-label]").textContent = selectedOptionText(select);
                    options.querySelectorAll(".admin-mobile-select-option").forEach((optionButton) => {
                        const selected = optionButton.dataset.value === select.value;
                        optionButton.classList.toggle("is-selected", selected);
                        optionButton.setAttribute("aria-selected", selected ? "true" : "false");
                    });
                };

                Array.from(select.options).forEach((option) => {
                    const optionButton = document.createElement("button");
                    optionButton.type = "button";
                    optionButton.className = "admin-mobile-select-option";
                    optionButton.dataset.value = option.value;
                    optionButton.setAttribute("role", "option");
                    optionButton.innerHTML = '<span></span><i class="bi bi-check-lg" aria-hidden="true"></i>';
                    optionButton.querySelector("span").textContent = option.textContent.trim();
                    optionButton.addEventListener("click", () => {
                        select.value = option.value;
                        select.dispatchEvent(new Event("change", { bubbles: true }));
                        sync();
                        closeMobileSelects();
                        toggle.focus();
                    });
                    options.append(optionButton);
                });

                toggle.addEventListener("click", () => {
                    if (!mobileQuery.matches) return;
                    const shouldOpen = !control.classList.contains("is-open");
                    closeMobileSelects(control);
                    control.classList.toggle("is-open", shouldOpen);
                    toggle.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
                    options.hidden = !shouldOpen;
                });

                select.addEventListener("change", sync);
                control.append(toggle, options);
                select.insertAdjacentElement("afterend", control);
                syncMobileSelectMode(select);
                sync();
            });
        };

        const enhanceVehicles = (scope) => {
            const container = scope.querySelector("#vozila-list-container");
            const toolbar = container?.querySelector(".admin-list-toolbar");
            const filters = container?.querySelector(".admin-vehicle-filters");
            const sort = container?.querySelector(".admin-mobile-sort");
            if (!container || !toolbar || !filters || !sort || container.dataset.mobileEnhanced === "true") return;
            container.dataset.mobileEnhanced = "true";

            prepareSheet(filters, filtersText, false);
            prepareSheet(sort, sortText, false);

            const tools = createTools();
            const filterButton = createToolButton(filtersText, "bi-funnel", () => openSheet(filters, filterButton));
            const sortButton = createToolButton(sortText, "bi-arrow-down-up", () => openSheet(sort, sortButton));
            tools.append(filterButton, sortButton);
            toolbar.insertAdjacentElement("afterend", tools);

            const activeFilters = document.createElement("div");
            activeFilters.className = "admin-mobile-active-filters";
            tools.insertAdjacentElement("afterend", activeFilters);

            const status = container.querySelector("#vehicle-status-filter");
            const fuel = container.querySelector("#vehicle-fuel-filter");
            const color = container.querySelector("#vehicle-color-filter");
            const colorPicker = color?.closest(".admin-color-filter");
            const sortSelect = container.querySelector("#vozilo-sort-select");
            const controls = [
                { select: status, reset: "available" },
                { select: fuel, reset: "" },
                { select: color, reset: "" }
            ];

            const markSourceSelect = (select) => {
                if (!select) return;
                select.classList.add("admin-mobile-native-select");
                syncMobileSelectMode(select);
            };

            const createChoiceGroup = (select, title, className) => {
                if (!select) return null;
                markSourceSelect(select);

                const group = document.createElement("section");
                group.className = `admin-mobile-choice-group ${className}`;
                const heading = document.createElement("span");
                heading.className = "admin-mobile-choice-label";
                heading.textContent = title;
                const choices = document.createElement("div");
                choices.className = "admin-mobile-choice-grid";
                choices.setAttribute("role", "listbox");

                const sync = () => {
                    choices.querySelectorAll(".admin-mobile-choice").forEach((choice) => {
                        const selected = choice.dataset.value === select.value;
                        choice.classList.toggle("is-selected", selected);
                        choice.setAttribute("aria-selected", selected ? "true" : "false");
                    });
                };

                Array.from(select.options).forEach((option) => {
                    const choice = document.createElement("button");
                    choice.type = "button";
                    choice.className = "admin-mobile-choice";
                    choice.dataset.value = option.value;
                    choice.setAttribute("role", "option");
                    choice.textContent = option.textContent.trim();
                    choice.addEventListener("click", () => {
                        select.value = option.value;
                        sync();
                        select.dispatchEvent(new Event("change", { bubbles: true }));
                    });
                    choices.append(choice);
                });

                select.addEventListener("change", sync);
                group.append(heading, choices);
                sync();
                return group;
            };

            const mobileFilterContent = document.createElement("div");
            mobileFilterContent.className = "admin-mobile-filter-content";
            const statusChoices = createChoiceGroup(status, statusText, "is-status");
            const fuelChoices = createChoiceGroup(fuel, fuelText, "is-fuel");
            if (statusChoices) mobileFilterContent.append(statusChoices);
            if (fuelChoices) mobileFilterContent.append(fuelChoices);

            if (color && colorPicker) {
                colorPicker.classList.add("admin-mobile-original-color-filter");

                const colorGroup = document.createElement("section");
                colorGroup.className = "admin-mobile-choice-group is-color";
                const colorHeading = document.createElement("span");
                colorHeading.className = "admin-mobile-choice-label";
                colorHeading.textContent = colorText;
                const colorTrigger = document.createElement("button");
                colorTrigger.type = "button";
                colorTrigger.className = "admin-mobile-color-trigger";
                colorTrigger.innerHTML = '<span class="admin-mobile-color-value"><span class="vehicle-color-swatch d-none" aria-hidden="true"></span><strong></strong></span><i class="bi bi-chevron-right" aria-hidden="true"></i>';

                const drilldown = document.createElement("div");
                drilldown.className = "admin-mobile-color-drilldown";
                drilldown.setAttribute("aria-hidden", "true");
                const drillHeader = document.createElement("div");
                drillHeader.className = "admin-mobile-drilldown-header";
                const backButton = document.createElement("button");
                backButton.type = "button";
                backButton.className = "admin-mobile-drilldown-back";
                backButton.setAttribute("aria-label", backText);
                backButton.innerHTML = '<i class="bi bi-arrow-left" aria-hidden="true"></i>';
                const drillTitle = document.createElement("strong");
                drillTitle.textContent = colorText;
                drillHeader.append(backButton, drillTitle);
                const colorOptions = document.createElement("div");
                colorOptions.className = "admin-mobile-color-options";
                colorOptions.setAttribute("role", "listbox");

                const closeColorDrilldown = (restoreFocus = true) => {
                    filters.classList.remove("is-drilldown-open");
                    drilldown.classList.remove("is-open");
                    drilldown.setAttribute("aria-hidden", "true");
                    if (restoreFocus) colorTrigger.focus();
                };

                const openColorDrilldown = () => {
                    filters.classList.add("is-drilldown-open");
                    drilldown.classList.add("is-open");
                    drilldown.setAttribute("aria-hidden", "false");
                    window.requestAnimationFrame(() => backButton.focus());
                };

                const sourceColorOptions = colorPicker.querySelectorAll("[data-color-option]");
                const syncColor = () => {
                    const selectedSource = Array.from(sourceColorOptions).find((option) => option.dataset.value === color.value);
                    const label = selectedSource?.dataset.label || selectedOptionText(color);
                    const swatchClass = selectedSource?.dataset.swatchClass || "";
                    const triggerSwatch = colorTrigger.querySelector(".vehicle-color-swatch");
                    triggerSwatch.className = `vehicle-color-swatch ${swatchClass || "d-none"}`;
                    colorTrigger.querySelector("strong").textContent = label;
                    colorOptions.querySelectorAll(".admin-mobile-color-option").forEach((option) => {
                        const selected = option.dataset.value === color.value;
                        option.classList.toggle("is-selected", selected);
                        option.setAttribute("aria-selected", selected ? "true" : "false");
                    });
                };

                sourceColorOptions.forEach((sourceOption) => {
                    const option = document.createElement("button");
                    option.type = "button";
                    option.className = "admin-mobile-color-option";
                    option.dataset.value = sourceOption.dataset.value || "";
                    option.setAttribute("role", "option");
                    const swatchClass = sourceOption.dataset.swatchClass || "";
                    option.innerHTML = `<span class="admin-mobile-color-option-label"><span class="vehicle-color-swatch ${swatchClass || "is-all"}" aria-hidden="true">${swatchClass ? "" : '<i class="bi bi-palette"></i>'}</span><span></span></span><i class="bi bi-check-lg" aria-hidden="true"></i>`;
                    option.querySelector(".admin-mobile-color-option-label > span:last-child").textContent = sourceOption.dataset.label || sourceOption.textContent.trim();
                    option.addEventListener("click", () => {
                        color.value = option.dataset.value;
                        syncColor();
                        closeColorDrilldown(false);
                        color.dispatchEvent(new Event("change", { bubbles: true }));
                    });
                    colorOptions.append(option);
                });

                colorTrigger.addEventListener("click", openColorDrilldown);
                backButton.addEventListener("click", () => closeColorDrilldown());
                color.addEventListener("change", syncColor);
                colorGroup.append(colorHeading, colorTrigger);
                drilldown.append(drillHeader, colorOptions);
                mobileFilterContent.append(colorGroup);
                filters.append(drilldown);
                syncColor();
            }

            filters.querySelector(".admin-mobile-sheet-header")?.insertAdjacentElement("afterend", mobileFilterContent);

            if (sortSelect) {
                markSourceSelect(sortSelect);
                sort.classList.add("is-direct-choice-sheet");
                const sortOptions = document.createElement("div");
                sortOptions.className = "admin-mobile-sort-options";
                sortOptions.setAttribute("role", "listbox");

                const syncSort = () => {
                    sortOptions.querySelectorAll(".admin-mobile-sort-option").forEach((option) => {
                        const selected = option.dataset.value === sortSelect.value;
                        option.classList.toggle("is-selected", selected);
                        option.setAttribute("aria-selected", selected ? "true" : "false");
                    });
                };

                Array.from(sortSelect.options).forEach((sourceOption) => {
                    const option = document.createElement("button");
                    option.type = "button";
                    option.className = "admin-mobile-sort-option";
                    option.dataset.value = sourceOption.value;
                    option.setAttribute("role", "option");
                    option.innerHTML = '<span></span><i class="bi bi-check-lg" aria-hidden="true"></i>';
                    option.querySelector("span").textContent = sourceOption.textContent.trim();
                    option.addEventListener("click", () => {
                        sortSelect.value = sourceOption.value;
                        syncSort();
                        sortSelect.dispatchEvent(new Event("change", { bubbles: true }));
                    });
                    sortOptions.append(option);
                });

                sortSelect.addEventListener("change", syncSort);
                sort.querySelector(".admin-mobile-sheet-header")?.insertAdjacentElement("afterend", sortOptions);
                syncSort();
            }

            const renderChips = () => {
                activeFilters.replaceChildren();
                controls.forEach(({ select, reset }) => {
                    if (!select || !select.value || select.value === reset) return;
                    const label = selectedOptionText(select);
                    const chip = document.createElement("div");
                    chip.className = "admin-mobile-filter-chip";
                    chip.innerHTML = '<span></span><button type="button" class="admin-mobile-filter-chip-remove"><i class="bi bi-x-lg" aria-hidden="true"></i></button>';
                    chip.querySelector("span").textContent = label;
                    const removeButton = chip.querySelector(".admin-mobile-filter-chip-remove");
                    removeButton.setAttribute("aria-label", `${removeText}: ${label}`);
                    removeButton.addEventListener("click", () => {
                        select.value = reset;
                        select.dispatchEvent(new Event("change", { bubbles: true }));
                    });
                    activeFilters.append(chip);
                });
            };

            controls.forEach(({ select }) => select?.addEventListener("change", renderChips));
            renderChips();
        };

        const enhanceReviews = (scope) => {
            const container = scope.querySelector("#recenzije-list-container");
            const form = container?.querySelector(".review-filter-form");
            const toolbar = container?.querySelector(".admin-toolbar-stacked");
            if (!container || !form || !toolbar || container.dataset.mobileEnhanced === "true") return;
            container.dataset.mobileEnhanced = "true";

            prepareSheet(form, filtersText, false);
            const tools = createTools();
            const filterButton = createToolButton(filtersText, "bi-funnel", () => openSheet(form, filterButton));
            tools.append(filterButton);
            toolbar.insertAdjacentElement("afterend", tools);
            form.querySelector("#recenzija-filter-search-button")?.addEventListener("click", () => closeSheet());
        };

        const enhanceSection = (scope = content) => {
            if (!scope) return;
            enhanceVehicles(scope);
            enhanceReviews(scope);
            enhanceMobileSelects(scope);
        };

        menuToggle?.addEventListener("click", () => {
            if (sidebar?.classList.contains("is-mobile-menu-open")) closeMenu(true);
            else openMenu();
        });
        menuBackdrop?.addEventListener("click", () => closeMenu(true));
        menuClose?.addEventListener("click", () => closeMenu(true));
        toolsBackdrop.addEventListener("click", () => closeSheet(true));
        addSwipeToClose(menu, () => closeMenu(true));

        root.addEventListener("click", (event) => {
            const link = event.target.closest(".admin-menu-link");
            if (!link || !root.contains(link)) return;
            updateCurrentSection(link);
            closeMenu();
        });

        $(document).on("admin:section-loaded.adminMobile", (_, sectionName) => {
            const activeLink = menu?.querySelector(`.admin-menu-link[data-section="${sectionName}"]`);
            updateCurrentSection(activeLink);
            closeMenu();
            closeSheet();
            enhanceSection(content);
        });

        document.addEventListener("keydown", (event) => {
            if (event.key !== "Escape") return;
            if (root.querySelector(".admin-mobile-select-control.is-open")) closeMobileSelects();
            else if (activeSheet && closeSheetDrilldown(activeSheet.element, true)) return;
            else if (activeSheet) closeSheet(true);
            else closeMenu(true);
        });

        document.addEventListener("click", (event) => {
            if (!event.target.closest(".admin-mobile-select-control")) closeMobileSelects();
        });

        const contentObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => mutation.addedNodes.forEach((node) => {
                if (!(node instanceof Element)) return;
                enhanceMobileSelects(node.matches("select.form-select") ? node.parentElement : node);
            }));
        });
        if (content) contentObserver.observe(content, { childList: true, subtree: true });

        const handleBreakpointChange = () => {
            root.querySelectorAll("select.admin-mobile-native-select").forEach(syncMobileSelectMode);
            if (!mobileQuery.matches) {
                closeMobileSelects();
                closeMenu();
                closeSheet();
                document.body.classList.remove("admin-mobile-overlay-open");
                root.querySelectorAll(".admin-mobile-sheet-panel").forEach((sheet) => sheet.setAttribute("aria-hidden", "false"));
                return;
            }

            root.querySelectorAll(".admin-mobile-sheet-panel:not(.is-open)").forEach((sheet) => sheet.setAttribute("aria-hidden", "true"));
        };
        mobileQuery.addEventListener?.("change", handleBreakpointChange);

        updateCurrentSection(menu?.querySelector(".admin-menu-link.active") || menu?.querySelector(".admin-menu-link"));
        enhanceSection(content);
    };

    if (window.jQuery) $(initialize);
    else document.addEventListener("DOMContentLoaded", initialize, { once: true });
})();
