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

    window.adminCore = {
        escapeHtml,
        escapeAttribute,
        errorMessage,
        parseUnobtrusiveValidation
    };
})();
