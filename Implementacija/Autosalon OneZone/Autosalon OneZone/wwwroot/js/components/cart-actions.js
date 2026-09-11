(() => {
    const minimumLoadingTime = 650;

    const showToast = (message, type, viewText, cartUrl) => {
        $(".app-toast").remove();

        const isError = type === "error";
        const $toast = $("<div>", {
            class: `app-toast ${isError ? "is-error" : "is-success"}`,
            role: isError ? "alert" : "status"
        });
        const $icon = $("<i>", {
            class: `bi ${isError ? "bi-exclamation-circle-fill" : "bi-check-circle-fill"}`,
            "aria-hidden": "true"
        });
        const $content = $("<div>", { class: "app-toast-content" })
            .append($("<span>").text(message));

        if (!isError) {
            $content.append($("<a>", { href: cartUrl, text: viewText }));
        }

        $toast.append($icon, $content).appendTo(document.body);
        window.requestAnimationFrame(() => $toast.addClass("is-visible"));

        window.setTimeout(() => {
            $toast.removeClass("is-visible");
            window.setTimeout(() => $toast.remove(), 340);
        }, 4200);
    };

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
                    showToast(result.message || form.dataset.errorText, "error", "", "");
                    return;
                }

                $button.removeClass("is-loading").addClass("is-added");
                $icon.attr("class", "bi bi-check-lg");
                $label.text(form.dataset.addedText);
                updateCartCount(result.cartCount);

                const message = result.alreadyAdded
                    ? result.message
                    : form.dataset.successTemplate.replace("{0}", form.dataset.vehicleName);
                showToast(message, "success", form.dataset.viewCartText, form.dataset.cartUrl);
            });
        }).fail((xhr) => {
            afterMinimumLoadingTime(startedAt, () => {
                $button.prop("disabled", false).removeClass("is-loading");
                $icon.attr("class", "bi bi-cart3");
                $label.text(originalText);
                showToast(xhr.responseJSON?.message || form.dataset.errorText, "error", "", "");
            });
        });
    });
})();
