(() => {
    if (window.adminCombobox) return;

    const initializeOne = (element) => {
        if (!element || element.dataset.initialized === "true") return;
        element.dataset.initialized = "true";

        const $root = $(element);
        const $input = $root.find("[data-combobox-input]").first();
        const $value = $root.find("[data-combobox-value]").first();
        const $menu = $root.find("[data-combobox-menu]").first();
        const $toggle = $root.find("[data-combobox-toggle]").first();
        const suggestionsUrl = element.dataset.suggestionsUrl || "";
        const menuId = $menu.attr("id");
        let activeIndex = -1;
        let activeRequest = null;
        let debounceTimer = null;

        const options = () => $menu.find("[data-combobox-option]");

        const prepareOptions = ($items = options()) => {
            $items.each((index, option) => {
                if (!option.id && menuId) option.id = `${menuId}-option-${index}`;
                if (!option.hasAttribute("aria-selected")) option.setAttribute("aria-selected", "false");
            });
        };

        const syncAction = () => {
            const hasValue = Boolean(String($input.val() || "").trim());
            const label = hasValue ? $toggle.data("clear-label") : $toggle.data("open-label");
            $toggle.attr("aria-label", label || "");
            $toggle.find("i")
                .toggleClass("bi-x-lg", hasValue)
                .toggleClass("bi-search", !hasValue);
        };

        const visibleOptions = () => options().filter(":not([hidden])");

        const setActive = (index) => {
            const $visible = visibleOptions();
            if (!$visible.length) {
                activeIndex = -1;
                $input.removeAttr("aria-activedescendant");
                return;
            }

            activeIndex = Math.max(0, Math.min(index, $visible.length - 1));
            options().removeClass("active");
            const active = $visible.get(activeIndex);
            active?.classList.add("active");
            if (active?.id) $input.attr("aria-activedescendant", active.id);
            active?.scrollIntoView({ block: "nearest" });
        };

        const close = () => {
            $menu.prop("hidden", true);
            $input.attr("aria-expanded", "false").removeAttr("aria-activedescendant");
            options().removeClass("active");
            activeIndex = -1;
        };

        const showMenu = () => {
            if (!visibleOptions().length) {
                close();
                return;
            }
            $menu.prop("hidden", false);
            $input.attr("aria-expanded", "true");
        };

        const filterOptions = () => {
            const query = String($input.val() || "").trim().toLocaleLowerCase();
            options().each((_, option) => {
                const label = String(option.dataset.label || option.textContent || "").toLocaleLowerCase();
                option.hidden = Boolean(query) && !label.includes(query);
            });
        };

        const openLocal = (showAll = false) => {
            if (showAll) options().prop("hidden", false);
            else filterOptions();
            showMenu();
        };

        const selectOption = (option, notify = true) => {
            if (!option) return;
            const value = option.dataset.value || "";
            const label = option.dataset.label || option.textContent.trim();
            $value.val(value);
            $input.val(value ? label : "");
            options().attr("aria-selected", "false");
            option.setAttribute("aria-selected", "true");
            syncAction();
            close();

            if (notify) {
                element.dispatchEvent(new CustomEvent("admin-combobox:selected", {
                    bubbles: true,
                    detail: { value, label }
                }));
            }
        };

        const renderRemoteOptions = (items, query) => {
            $menu.find("[data-combobox-option]:not([data-combobox-static])").remove();
            $menu.find("[data-combobox-static]").prop("hidden", Boolean(String(query || "").trim()));

            (Array.isArray(items) ? items : []).forEach((item) => {
                const option = document.createElement("button");
                option.type = "button";
                option.className = "admin-combobox-option";
                option.setAttribute("role", "option");
                option.setAttribute("data-combobox-option", "");
                option.dataset.value = String(item?.value ?? "");
                option.dataset.label = String(item?.label ?? "");
                option.textContent = option.dataset.label;
                $menu.append(option);
            });

            prepareOptions();
        };

        const loadRemoteOptions = (query = "", selectedId = "") => {
            if (!suggestionsUrl) return;
            activeRequest?.abort();
            const request = window.appApi.get(suggestionsUrl, { query, selectedId })
                .done((response) => {
                    renderRemoteOptions(response?.options, query);
                    if (selectedId) {
                        const selected = options().toArray()
                            .find((option) => option.dataset.value === String(selectedId));
                        if (selected) selectOption(selected, false);
                        else $value.val("");
                        return;
                    }

                    if (document.activeElement === $input[0]) showMenu();
                })
                .fail((_, statusText) => {
                    if (statusText !== "abort") close();
                })
                .always(() => {
                    if (activeRequest === request) activeRequest = null;
                });
            activeRequest = request;
        };

        const scheduleRemoteOptions = () => {
            window.clearTimeout(debounceTimer);
            debounceTimer = window.setTimeout(() => loadRemoteOptions($input.val()), 180);
        };

        element.adminComboboxSetValue = (value, fallbackText = "") => {
            const normalizedValue = String(value || "");
            const option = options().toArray().find((item) => item.dataset.value === normalizedValue);
            if (option) {
                selectOption(option, false);
                return;
            }

            $value.val(normalizedValue);
            $input.val(fallbackText || "");
            syncAction();
            close();
            if (suggestionsUrl && normalizedValue) loadRemoteOptions("", normalizedValue);
        };

        $input.on("input.adminCombobox", () => {
            const selected = options().toArray()
                .find((option) => option.dataset.value === String($value.val() || ""));
            if (!selected || String($input.val() || "") !== String(selected.dataset.label || "")) {
                $value.val("");
                options().attr("aria-selected", "false");
            }
            syncAction();
            if (suggestionsUrl) scheduleRemoteOptions();
            else openLocal();
        });

        $input.on("focus.adminCombobox", () => {
            if (suggestionsUrl) {
                window.clearTimeout(debounceTimer);
                const selectedId = $input.val() ? "" : $value.val();
                loadRemoteOptions($input.val(), selectedId);
            } else {
                openLocal();
            }
        });

        $toggle.on("click.adminCombobox", () => {
            if (String($input.val() || "").trim()) {
                window.clearTimeout(debounceTimer);
                activeRequest?.abort();
                $input.val("");
                $value.val("");
                options().attr("aria-selected", "false");
                syncAction();
                close();
                element.dispatchEvent(new CustomEvent("admin-combobox:cleared", { bubbles: true }));
                $input[0]?.focus({ preventScroll: true });
                return;
            }

            if (!$menu.prop("hidden")) {
                close();
                return;
            }

            const alreadyFocused = document.activeElement === $input[0];
            $input[0]?.focus({ preventScroll: true });
            if (alreadyFocused) {
                if (suggestionsUrl) loadRemoteOptions("");
                else openLocal(true);
            }
        });

        $input.on("keydown.adminCombobox", (event) => {
            const $visible = visibleOptions();
            if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                event.preventDefault();
                if ($menu.prop("hidden")) {
                    if (suggestionsUrl) {
                        loadRemoteOptions($input.val());
                        return;
                    }
                    openLocal();
                }
                const offset = event.key === "ArrowDown" ? 1 : -1;
                setActive(activeIndex < 0 ? (offset > 0 ? 0 : $visible.length - 1) : activeIndex + offset);
                return;
            }
            if (event.key === "Enter" && activeIndex >= 0) {
                event.preventDefault();
                selectOption(visibleOptions().get(activeIndex));
                return;
            }
            if (event.key === "Escape") {
                event.preventDefault();
                close();
            }
        });

        $menu.on("mousedown.adminCombobox", "[data-combobox-option]", (event) => event.preventDefault());
        $menu.on("click.adminCombobox", "[data-combobox-option]", function () {
            selectOption(this);
        });
        $root.on("admin-combobox:close.adminCombobox", close);
        prepareOptions();
        syncAction();
    };

    const initialize = (scope = document) => {
        const elements = scope.matches?.("[data-admin-combobox]")
            ? [scope]
            : [...scope.querySelectorAll("[data-admin-combobox]")];
        elements.forEach(initializeOne);
    };

    const setValue = (element, value, fallbackText = "") => {
        const target = typeof element === "string" ? document.querySelector(element) : element;
        initializeOne(target);
        target?.adminComboboxSetValue?.(value, fallbackText);
    };

    $(document).on("mousedown.adminCombobox", (event) => {
        $("[data-admin-combobox]").each((_, element) => {
            if (!element.contains(event.target)) {
                $(element).triggerHandler("admin-combobox:close");
            }
        });
    });

    window.adminCombobox = { initialize, setValue };
})();
