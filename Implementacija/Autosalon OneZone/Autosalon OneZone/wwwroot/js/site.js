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
    if (window.imagePreviewInitialized) {
        return;
    }

    window.imagePreviewInitialized = true;

    document.addEventListener("click", function (event) {
        const trigger = event.target.closest("[data-image-preview]");
        if (!trigger || !window.bootstrap) return;

        const modalElement = document.getElementById("globalImagePreviewModal");
        const image = document.getElementById("globalImagePreview");
        const source = trigger.dataset.imageSrc || trigger.querySelector("img")?.currentSrc;
        if (!modalElement || !image || !source) return;

        event.preventDefault();
        image.src = source;
        image.alt = trigger.querySelector("img")?.alt || "";
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    });

    document.addEventListener("DOMContentLoaded", function () {
        const modalElement = document.getElementById("globalImagePreviewModal");
        const image = document.getElementById("globalImagePreview");
        if (!modalElement || !image) return;

        modalElement.addEventListener("hidden.bs.modal", () => image.removeAttribute("src"));
    });
})();

(() => {
    if (window.numberInputWheelGuardInitialized) {
        return;
    }

    window.numberInputWheelGuardInitialized = true;

    document.addEventListener("wheel", function () {
        const activeElement = document.activeElement;
        if (activeElement instanceof HTMLInputElement && activeElement.type === "number") {
            activeElement.blur();
        }
    }, { capture: true });
})();

(() => {
    if (window.showAppToast) {
        return;
    }

    const toastIcons = {
        success: "bi-check-circle-fill",
        error: "bi-exclamation-circle-fill",
        danger: "bi-exclamation-circle-fill",
        info: "bi-info-circle-fill",
        warning: "bi-exclamation-triangle-fill"
    };

    let activeToast = null;
    let closeTimer = null;

    const removeToast = () => {
        if (closeTimer) {
            window.clearTimeout(closeTimer);
            closeTimer = null;
        }

        if (!activeToast) {
            return;
        }

        activeToast.classList.remove("is-visible");
        const toastToRemove = activeToast;
        activeToast = null;
        window.setTimeout(() => toastToRemove.remove(), 340);
    };

    window.showAppToast = function (message, options = {}) {
        if (!message) {
            return;
        }

        if (typeof options === "string") {
            options = { type: options };
        }

        const type = options.type || "success";
        const duration = Number.isFinite(options.duration) ? options.duration : 4200;
        removeToast();

        const toast = document.createElement("div");
        toast.className = `app-toast is-${type === "danger" ? "error" : type}`;
        toast.setAttribute("role", type === "error" || type === "danger" ? "alert" : "status");
        toast.setAttribute("aria-live", type === "error" || type === "danger" ? "assertive" : "polite");

        const icon = document.createElement("i");
        icon.className = `bi ${toastIcons[type] || toastIcons.info}`;
        icon.setAttribute("aria-hidden", "true");

        const content = document.createElement("div");
        content.className = "app-toast-content";

        const text = document.createElement("span");
        text.textContent = message;
        content.appendChild(text);

        if (options.actionText && options.actionHref) {
            const actionLink = document.createElement("a");
            actionLink.href = options.actionHref;
            actionLink.textContent = options.actionText;
            content.appendChild(actionLink);
        } else if (options.actionText && typeof options.onAction === "function") {
            const actionButton = document.createElement("button");
            actionButton.type = "button";
            actionButton.textContent = options.actionText;
            actionButton.addEventListener("click", () => options.onAction(actionButton));
            content.appendChild(actionButton);
        }

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = "app-toast-close";
        closeButton.setAttribute("aria-label", (window.appTexts && window.appTexts.close) || "Close");
        closeButton.innerHTML = "&times;";
        closeButton.addEventListener("click", removeToast);

        toast.append(icon, content, closeButton);
        document.body.appendChild(toast);
        activeToast = toast;

        window.requestAnimationFrame(() => toast.classList.add("is-visible"));
        closeTimer = window.setTimeout(removeToast, duration);
    };

    window.dismissAppToast = removeToast;

    document.addEventListener("DOMContentLoaded", () => {
        (window.appInitialToasts || []).forEach(({ message, type }) => {
            if (message) {
                window.showAppToast(message, { type });
            }
        });
    });
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
    if (window.updateCartBadge) {
        return;
    }

    window.updateCartBadge = function (count) {
        const normalizedCount = Math.max(0, Number.parseInt(count, 10) || 0);
        document.querySelectorAll("[data-cart-count]").forEach((badge) => {
            badge.textContent = String(normalizedCount);
            badge.classList.toggle("d-none", normalizedCount < 1);
        });
    };
})();

(() => {
    const storageKey = "autosalon-language-switch-scroll-v1";
    const currentLocation = () => `${window.location.pathname}${window.location.search}${window.location.hash}`;

    try {
        const savedState = sessionStorage.getItem(storageKey);

        if (savedState) {
            sessionStorage.removeItem(storageKey);
            const { returnUrl, scrollY } = JSON.parse(savedState);

            if (returnUrl === currentLocation() && Number.isFinite(scrollY)) {
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
            const returnUrl = currentLocation();
            const returnUrlInput = form.querySelector('input[name="returnUrl"]');
            if (returnUrlInput) {
                returnUrlInput.value = returnUrl;
            }

            try {
                sessionStorage.setItem(storageKey, JSON.stringify({
                    returnUrl,
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
