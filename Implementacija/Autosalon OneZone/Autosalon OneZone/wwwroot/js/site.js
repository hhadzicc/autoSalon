
(() => {
    if (window.deleteConfirmationInitialized) {
        return;
    }

    window.deleteConfirmationInitialized = true;

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

        titleElement.textContent = trigger.getAttribute("data-confirm-title") || "Potvrda brisanja";
        messageElement.textContent = trigger.getAttribute("data-confirm-message") || "Da li ste sigurni da želite obrisati ovu stavku?";
        confirmButton.textContent = trigger.getAttribute("data-confirm-action") || "Da, obriši";

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
        closeButton.setAttribute("aria-label", "Zatvori");
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
