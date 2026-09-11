(() => {
    if (window.deleteConfirmationInitialized) {
        return;
    }

    window.deleteConfirmationInitialized = true;

    const appTexts = window.appTexts || {};
    let pendingTrigger = null;
    let pendingForm = null;
    let pendingLink = null;

    function getElement(id) {
        return document.getElementById(id);
    }

    document.addEventListener("click", function (event) {
        const trigger = event.target.closest("[data-confirm-delete='true']");
        if (!trigger) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const modalElement = getElement("globalDeleteConfirmModal");
        const titleElement = getElement("globalDeleteConfirmTitle");
        const messageElement = getElement("globalDeleteConfirmMessage");
        const itemElement = getElement("globalDeleteConfirmItem");
        const confirmButton = getElement("globalDeleteConfirmButton");

        if (!modalElement || !titleElement || !messageElement || !itemElement || !confirmButton || !window.bootstrap) {
            return;
        }

        pendingTrigger = trigger;
        pendingForm = trigger.closest("form");
        pendingLink = !pendingForm && trigger.tagName === "A" ? trigger.href : null;

        titleElement.textContent = trigger.getAttribute("data-confirm-title") || appTexts.deleteConfirmTitle || "Delete confirmation";
        messageElement.textContent = trigger.getAttribute("data-confirm-message") || appTexts.deleteConfirmMessage || "Are you sure you want to delete this item?";
        confirmButton.textContent = trigger.getAttribute("data-confirm-action") || appTexts.deleteConfirmAction || "Yes, delete";

        const item = trigger.getAttribute("data-confirm-item") || "";
        if (item.trim()) {
            itemElement.textContent = item;
            itemElement.classList.remove("d-none");
        } else {
            itemElement.textContent = "";
            itemElement.classList.add("d-none");
        }

        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }, true);

    document.addEventListener("DOMContentLoaded", function () {
        const confirmButton = getElement("globalDeleteConfirmButton");
        const modalElement = getElement("globalDeleteConfirmModal");

        if (!confirmButton || !modalElement || !window.bootstrap) {
            return;
        }

        confirmButton.addEventListener("click", function () {
            const trigger = pendingTrigger;
            const form = pendingForm;
            const link = pendingLink;
            const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

            pendingTrigger = null;
            pendingForm = null;
            pendingLink = null;

            modal.hide();

            if (trigger) {
                trigger.dispatchEvent(new CustomEvent("app:delete-confirmed", {
                    bubbles: true,
                    detail: { trigger: trigger }
                }));
            }

            if (form && trigger && !trigger.hasAttribute("data-confirm-event-only")) {
                form.submit();
                return;
            }

            if (link && trigger && !trigger.hasAttribute("data-confirm-event-only")) {
                window.location.href = link;
            }
        });
    });
})();

(() => {
    if (window.showAppFlashMessage) {
        return;
    }

    const alertClasses = {
        success: "alert-success",
        error: "alert-danger",
        danger: "alert-danger",
        info: "alert-info",
        warning: "alert-warning"
    };

    window.showAppFlashMessage = function (message, type = "success") {
        if (!message) {
            return;
        }

        const main = document.querySelector(".main-content-container") || document.body;
        let wrapper = document.querySelector(".flash-messages");

        if (!wrapper) {
            wrapper = document.createElement("div");
            wrapper.className = "flash-messages";
            wrapper.setAttribute("aria-live", "polite");
            main.insertBefore(wrapper, main.firstChild);
        }

        const alert = document.createElement("div");
        alert.className = `alert ${alertClasses[type] || alertClasses.success} alert-dismissible fade show app-alert`;
        alert.setAttribute("role", "alert");

        const text = document.createElement("span");
        text.textContent = message;
        alert.appendChild(text);

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = "btn-close";
        closeButton.setAttribute("data-bs-dismiss", "alert");
        closeButton.setAttribute("aria-label", (window.appTexts && window.appTexts.close) || "Close");
        alert.appendChild(closeButton);

        wrapper.replaceChildren(alert);

        if (window.bootstrap) {
            window.setTimeout(() => {
                const instance = bootstrap.Alert.getOrCreateInstance(alert);
                instance.close();
            }, 4500);
        }
    };
})();

(() => {
    if (window.passwordToggleInitialized) {
        return;
    }

    window.passwordToggleInitialized = true;

    document.addEventListener("click", function (event) {
        const button = event.target.closest(".password-toggle");
        if (!button) {
            return;
        }

        const wrapper = button.closest(".password-wrapper");
        const input = wrapper ? wrapper.querySelector(".password-input") : null;
        const icon = button.querySelector("i");

        if (!input || !icon) {
            return;
        }

        const texts = window.appTexts || {};
        const isPassword = input.getAttribute("type") === "password";

        input.setAttribute("type", isPassword ? "text" : "password");
        icon.classList.toggle("bi-eye", !isPassword);
        icon.classList.toggle("bi-eye-slash", isPassword);
        button.setAttribute("aria-label", isPassword ? (texts.hidePassword || "Hide password") : (texts.showPassword || "Show password"));
    });
})();

(() => {
    const storageKey = "autosalon-language-switch-scroll-v1";
    const currentLocation = `${window.location.pathname}${window.location.search}`;

    try {
        const savedState = sessionStorage.getItem(storageKey);

        if (savedState) {
            sessionStorage.removeItem(storageKey);
            const { returnUrl, scrollY } = JSON.parse(savedState);

            if (returnUrl === currentLocation && Number.isFinite(scrollY)) {
                if ("scrollRestoration" in history) {
                    history.scrollRestoration = "manual";
                }

                window.requestAnimationFrame(() => {
                    window.requestAnimationFrame(() => window.scrollTo(0, scrollY));
                });
            }
        }
    } catch {
        sessionStorage.removeItem(storageKey);
    }

    document.querySelectorAll(".language-switch-form").forEach(form => {
        form.addEventListener("submit", () => {
            try {
                sessionStorage.setItem(storageKey, JSON.stringify({
                    returnUrl: currentLocation,
                    scrollY: window.scrollY
                }));
            } catch {
                // Language switching still works when sessionStorage is unavailable.
            }
        });
    });
})();

(() => {
    if (window.validationMessagesInitialized || !window.jQuery || !window.jQuery.validator || !window.appTexts) {
        return;
    }

    window.validationMessagesInitialized = true;

    const texts = window.appTexts;
    $.extend($.validator.messages, {
        required: texts.validationRequired,
        email: texts.validationEmail,
        url: texts.validationUrl,
        date: texts.validationDate,
        dateISO: texts.validationDateIso,
        number: texts.validationNumber,
        digits: texts.validationDigits,
        creditcard: texts.validationCreditCard,
        equalTo: texts.validationEqualTo,
        maxlength: $.validator.format(texts.validationMaxLength),
        minlength: $.validator.format(texts.validationMinLength),
        rangelength: $.validator.format(texts.validationRangeLength),
        range: $.validator.format(texts.validationRange),
        max: $.validator.format(texts.validationMax),
        min: $.validator.format(texts.validationMin)
    });
})();
