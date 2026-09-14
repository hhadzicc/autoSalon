(() => {
    if (window.adminFilterControls) return;

    const initializeOne = (element) => {
        if (!element || element.dataset.initialized === "true") return;
        element.dataset.initialized = "true";

        const $root = $(element);
        const $input = $root.find("[data-clearable-input]").first();
        const $action = $root.find("[data-clearable-action]").first();

        const sync = () => {
            const hasValue = Boolean(String($input.val() || "").trim());
            const label = hasValue ? $action.data("clear-label") : $action.data("open-label");
            $action.attr("aria-label", label || "");
            $action.find("i")
                .toggleClass("bi-x-lg", hasValue)
                .toggleClass("bi-search", !hasValue);
        };

        $input.on("input.adminFilterControls", sync);
        $action.on("click.adminFilterControls", () => {
            if (String($input.val() || "").trim()) {
                $input.val("");
                sync();
                element.dispatchEvent(new CustomEvent("admin-filter:cleared", { bubbles: true }));
            }
            $input.trigger("focus");
        });

        element.adminFilterControlSync = sync;
        sync();
    };

    const initialize = (scope = document) => {
        const elements = scope.matches?.("[data-admin-clearable]")
            ? [scope]
            : [...scope.querySelectorAll("[data-admin-clearable]")];
        elements.forEach(initializeOne);
    };

    const sync = (scope = document) => {
        initialize(scope);
        const elements = scope.matches?.("[data-admin-clearable]")
            ? [scope]
            : [...scope.querySelectorAll("[data-admin-clearable]")];
        elements.forEach((element) => element.adminFilterControlSync?.());
    };

    window.adminFilterControls = { initialize, sync };
})();
