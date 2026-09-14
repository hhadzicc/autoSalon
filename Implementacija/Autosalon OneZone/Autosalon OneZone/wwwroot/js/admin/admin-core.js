(() => {
    if (window.adminCore) {
        return;
    }

    const escapeHtml = (value) => {
        const element = document.createElement("div");
        element.textContent = value ?? "";
        return element.innerHTML;
    };

    const escapeAttribute = (value) => escapeHtml(value)
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");

    const errorMessage = (xhr, fallback = "Error") => {
        if (window.appApi) {
            return window.appApi.getErrorMessage(xhr, fallback);
        }

        return xhr?.responseJSON?.message || xhr?.responseText || fallback;
    };

    const parseUnobtrusiveValidation = (form) => {
        const $form = $(form);
        if (!$form.length || !$.validator?.unobtrusive) {
            return;
        }

        $form.removeData("validator");
        $form.removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse($form);
    };

    const validationMessage = (form, fieldName) => $(form)
        .find("[data-valmsg-for]")
        .filter((_, element) => element.getAttribute("data-valmsg-for") === fieldName)
        .first();

    const clearFieldValidationError = (form, fieldName) => {
        const fields = form?.elements?.namedItem(fieldName);
        if (fields) {
            $(fields).removeClass("is-invalid").removeAttr("aria-invalid");
        }

        $(form).find(`[data-validation-proxy-for="${fieldName}"]`)
            .removeClass("is-invalid")
            .removeAttr("aria-invalid");

        validationMessage(form, fieldName)
            .empty()
            .removeClass("field-validation-error")
            .addClass("field-validation-valid");
    };

    const clearValidationErrors = (form) => {
        if (!form) return;

        $(form).find("[data-valmsg-for]").each((_, element) => {
            clearFieldValidationError(form, element.getAttribute("data-valmsg-for"));
        });
    };

    const applyValidationErrors = (form, errors = {}) => {
        const unmatched = [];
        let firstInvalidField = null;

        Object.entries(errors).forEach(([fieldName, fieldErrors]) => {
            const messages = Array.isArray(fieldErrors) ? fieldErrors.filter(Boolean) : [fieldErrors].filter(Boolean);
            if (!messages.length) return;

            const fields = form?.elements?.namedItem(fieldName);
            const $message = validationMessage(form, fieldName);
            if (!fields || !$message.length) {
                unmatched.push(...messages.map(String));
                return;
            }

            const $fields = $(fields);
            $fields.addClass("is-invalid").attr("aria-invalid", "true");
            $(form).find(`[data-validation-proxy-for="${fieldName}"]`)
                .addClass("is-invalid")
                .attr("aria-invalid", "true");
            $message
                .text(messages.join(" "))
                .removeClass("field-validation-valid")
                .addClass("field-validation-error");
            firstInvalidField ??= $fields.get(0);
        });

        firstInvalidField?.scrollIntoView({ behavior: "smooth", block: "center" });
        return unmatched;
    };

    const setLoading = (element, isLoading) => {
        if (!element) return;

        element.classList.toggle("admin-is-loading", isLoading);
        element.setAttribute("aria-busy", String(isLoading));
    };

    const scrollToElement = (element) => {
        if (!element) return;

        window.requestAnimationFrame(() => {
            element.scrollIntoView({ behavior: "smooth", block: "start" });
        });
    };

    const removeRenderedItem = (root, itemId, renderEmptyState) => {
        if (!root) return;

        const normalizedId = String(itemId);
        const items = [...root.querySelectorAll("[data-admin-item-id]")]
            .filter((element) => element.dataset.adminItemId === normalizedId);

        items.forEach((element) => element.classList.add("admin-item-removing"));
        window.setTimeout(() => {
            items.forEach((element) => element.remove());
            const hasRows = root.querySelector("tbody [data-admin-item-id]");
            if (!hasRows && typeof renderEmptyState === "function") {
                renderEmptyState();
            }
        }, 180);
    };

    window.adminCore = {
        escapeHtml,
        escapeAttribute,
        errorMessage,
        parseUnobtrusiveValidation,
        clearFieldValidationError,
        clearValidationErrors,
        applyValidationErrors,
        setLoading,
        scrollToElement,
        removeRenderedItem
    };
})();
