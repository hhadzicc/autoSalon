(() => {
    const findField = (form, baseName) => form.querySelector(`[name="${baseName}"]`);

    const getFieldId = (field) => field?.id || "";

    const clearFieldError = (field) => {
        if (!field) return;

        field.classList.remove("is-invalid", "border-danger");
        const error = document.getElementById(`${getFieldId(field)}Error`);
        if (error) error.textContent = "";
    };

    const showFieldError = (field, message) => {
        if (!field) return;

        field.classList.add("is-invalid", "border-danger");
        const error = document.getElementById(`${getFieldId(field)}Error`);
        if (error) error.textContent = Array.isArray(message) ? message.join(" ") : String(message || "");
    };

    const clearErrors = (form, errorTarget) => {
        form.querySelectorAll(".form-control").forEach(clearFieldError);
        if (errorTarget) {
            errorTarget.textContent = "";
            errorTarget.classList.add("d-none");
        }
    };

    const showRequestError = (errorTarget, message) => {
        if (!errorTarget) return;
        errorTarget.textContent = message;
        errorTarget.classList.remove("d-none");
    };

    const validate = (form, texts) => {
        const holder = findField(form, "ImeVlasnika");
        const card = findField(form, "BrojKartice");
        const expiry = findField(form, "DatumIsteka");
        const cvv = findField(form, "Cvv");
        let isValid = true;

        if (!holder?.value.trim()) {
            showFieldError(holder, texts.holderRequired);
            isValid = false;
        }

        const cardNumber = card?.value.replace(/\s+/g, "") || "";
        if (!cardNumber) {
            showFieldError(card, texts.cardRequired);
            isValid = false;
        } else if (!/^\d{16}$/.test(cardNumber)) {
            showFieldError(card, texts.cardLength);
            isValid = false;
        }

        const expiryValue = expiry?.value.trim() || "";
        if (!expiryValue) {
            showFieldError(expiry, texts.expiryRequired);
            isValid = false;
        } else {
            const parts = expiryValue.split("/");
            if (parts.length !== 2 || !/^(0[1-9]|1[0-2])$/.test(parts[0]) || !/^\d{2}$/.test(parts[1])) {
                showFieldError(expiry, texts.expiryFormat);
                isValid = false;
            } else {
                const month = Number.parseInt(parts[0], 10);
                const year = 2000 + Number.parseInt(parts[1], 10);
                const now = new Date();
                if (year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1)) {
                    showFieldError(expiry, texts.cardExpired);
                    isValid = false;
                }
            }
        }

        const cvvValue = cvv?.value.trim() || "";
        if (!cvvValue) {
            showFieldError(cvv, texts.cvvRequired);
            isValid = false;
        } else if (!/^\d{3,4}$/.test(cvvValue)) {
            showFieldError(cvv, texts.cvvLength);
            isValid = false;
        }

        if (!isValid) {
            form.querySelector(".is-invalid")?.focus();
        }

        return isValid;
    };

    const bindFieldFormatting = (form) => {
        const holder = findField(form, "ImeVlasnika");
        const card = findField(form, "BrojKartice");
        const expiry = findField(form, "DatumIsteka");
        const cvv = findField(form, "Cvv");

        holder?.addEventListener("input", () => {
            holder.value = holder.value.replace(/[^\p{L}\s'-]/gu, "");
            clearFieldError(holder);
        });

        card?.addEventListener("input", () => {
            const digits = card.value.replace(/\D/g, "").slice(0, 16);
            card.value = digits.replace(/(.{4})/g, "$1 ").trim();
            clearFieldError(card);
        });

        expiry?.addEventListener("input", () => {
            const digits = expiry.value.replace(/\D/g, "").slice(0, 4);
            expiry.value = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
            clearFieldError(expiry);
        });

        cvv?.addEventListener("input", () => {
            cvv.value = cvv.value.replace(/\D/g, "").slice(0, 4);
            clearFieldError(cvv);
        });
    };

    const setSubmitting = (button, isSubmitting, texts) => {
        if (!button) return;

        button.disabled = isSubmitting;
        button.innerHTML = isSubmitting
            ? `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> ${texts.processing}`
            : texts.confirmPurchase;
    };

    const showServerErrors = (form, errors) => {
        Object.entries(errors || {}).forEach(([fieldId, message]) => {
            showFieldError(document.getElementById(fieldId) || form.querySelector(`[name="${fieldId}"]`), message);
        });
        form.querySelector(".is-invalid")?.focus();
    };

    const initialize = (formOrSelector, options) => {
        const form = typeof formOrSelector === "string" ? document.querySelector(formOrSelector) : formOrSelector;
        if (!form || form.dataset.paymentFormInitialized === "true") return;

        const button = document.querySelector(options.buttonSelector);
        const errorTarget = document.querySelector(options.errorSelector);
        form.dataset.paymentFormInitialized = "true";
        bindFieldFormatting(form);

        form.addEventListener("submit", (event) => {
            event.preventDefault();
            clearErrors(form, errorTarget);

            if (options.beforeSubmit && options.beforeSubmit(form) === false) return;
            if (!validate(form, options.texts)) return;

            setSubmitting(button, true, options.texts);
            window.appApi.postForm(form)
                .done((response) => {
                    if (response?.success && response.redirectUrl) {
                        window.location.href = response.redirectUrl;
                        return;
                    }

                    if (response?.errors) {
                        showServerErrors(form, response.errors);
                    } else {
                        showRequestError(errorTarget, options.texts.paymentFailed + (response?.message || ""));
                    }
                    setSubmitting(button, false, options.texts);
                })
                .fail((xhr) => {
                    showRequestError(errorTarget, window.appApi.getErrorMessage(xhr, options.texts.requestError));
                    setSubmitting(button, false, options.texts);
                });
        });
    };

    window.paymentForm = {
        initialize,
        clear(formOrSelector, options) {
            const form = typeof formOrSelector === "string" ? document.querySelector(formOrSelector) : formOrSelector;
            if (!form) return;

            clearErrors(form, document.querySelector(options.errorSelector));
        },
        reset(formOrSelector, options) {
            const form = typeof formOrSelector === "string" ? document.querySelector(formOrSelector) : formOrSelector;
            if (!form) return;

            form.reset();
            clearErrors(form, document.querySelector(options.errorSelector));
            setSubmitting(document.querySelector(options.buttonSelector), false, options.texts);
        }
    };
})();
