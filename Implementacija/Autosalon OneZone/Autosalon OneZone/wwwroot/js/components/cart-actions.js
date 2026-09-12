(() => {
    const minimumLoadingTime = 650;

    const updateCartCount = (count) => {
        const $badge = $("[data-cart-count]");
        if ($badge.length === 0) {
            return;
        }

        $badge.text(count).toggleClass("d-none", count < 1);
    };

    const afterMinimumLoadingTime = (startedAt, callback) => {
        const elapsed = Date.now() - startedAt;
        window.setTimeout(callback, Math.max(0, minimumLoadingTime - elapsed));
    };

    $(document).on("submit", ".add-to-cart-form", function (event) {
        event.preventDefault();

        const form = this;
        const $form = $(form);
        const $button = $form.find("[data-cart-button]");
        const $icon = $form.find("[data-cart-icon]");
        const $label = $form.find("[data-cart-button-text]");
        const originalText = $label.text();
        const startedAt = Date.now();

        $button.prop("disabled", true).addClass("is-loading");
        $icon.attr("class", "bi bi-arrow-repeat");
        $label.text(form.dataset.addingText);

        $.ajax({
            url: form.action,
            type: "POST",
            data: $form.serialize(),
            dataType: "json",
            headers: {
                "Accept": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            }
        }).done((result) => {
            afterMinimumLoadingTime(startedAt, () => {
                if (!result.success) {
                    $button.prop("disabled", false).removeClass("is-loading");
                    $icon.attr("class", "bi bi-cart3");
                    $label.text(originalText);
                    window.showAppToast(result.message || form.dataset.errorText, { type: "error" });
                    return;
                }

                $button.removeClass("is-loading").addClass("is-added");
                $icon.attr("class", "bi bi-check-lg");
                $label.text(form.dataset.addedText);
                updateCartCount(result.cartCount);

                const message = result.alreadyAdded
                    ? result.message
                    : form.dataset.successTemplate.replace("{0}", form.dataset.vehicleName);
                window.showAppToast(message, {
                    type: "success",
                    actionText: form.dataset.viewCartText,
                    actionHref: form.dataset.cartUrl
                });
            });
        }).fail((xhr) => {
            afterMinimumLoadingTime(startedAt, () => {
                $button.prop("disabled", false).removeClass("is-loading");
                $icon.attr("class", "bi bi-cart3");
                $label.text(originalText);
                window.showAppToast(xhr.responseJSON?.message || form.dataset.errorText, { type: "error" });
            });
        });
    });
})();
