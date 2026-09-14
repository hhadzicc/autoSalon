(() => {
    if (window.vehicleColorPicker) return;

    const sync = (picker) => {
        const control = picker.querySelector("[data-option-select], [data-color-select]");
        if (!control) return;

        const options = [...picker.querySelectorAll("[data-option], [data-color-option]")];
        const selectedOption = options.find((option) => option.dataset.value === control.value);
        const swatch = picker.querySelector("[data-selected-color-swatch]");
        const label = picker.querySelector("[data-selected-option-label], [data-selected-color-label]");

        options.forEach((option) => option.setAttribute("aria-pressed", option === selectedOption ? "true" : "false"));
        if (label) label.textContent = selectedOption?.dataset.label || picker.dataset.placeholder || "";
        if (swatch) {
            swatch.className = `vehicle-color-swatch ${selectedOption?.dataset.swatchClass || "d-none"}`;
        }
    };

    const initialize = (root = document) => {
        root.querySelectorAll("[data-option-picker], [data-color-picker]").forEach((picker) => {
            if (picker.dataset.colorPickerInitialized === "true") return;
            picker.dataset.colorPickerInitialized = "true";

            const control = picker.querySelector("[data-option-select], [data-color-select]");
            if (!control) return;

            picker.addEventListener("click", (event) => {
                const option = event.target.closest("[data-option], [data-color-option]");
                if (!option || !picker.contains(option)) return;

                control.value = option.dataset.value || "";
                control.dispatchEvent(new Event("change", { bubbles: true }));
                sync(picker);
            });
            control.addEventListener("change", () => sync(picker));
            sync(picker);
        });
    };

    window.vehicleColorPicker = { initialize, sync };
    document.addEventListener("DOMContentLoaded", () => initialize());
})();
