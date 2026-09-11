(() => {
    const form = document.querySelector("form[data-account-validation]");
    if (!form) {
        return;
    }

    // These forms use Unicode-aware validation below. Removing the generated
    // adapters prevents JavaScript from misreading the server's \p{L} regex.
    form.querySelectorAll("[data-account-field]").forEach(input => {
        Array.from(input.attributes)
            .filter(attribute => attribute.name === "data-val" || attribute.name.startsWith("data-val-"))
            .forEach(attribute => input.removeAttribute(attribute.name));
    });

    const summary = form.previousElementSibling?.classList.contains("account-validation-summary")
        ? form.previousElementSibling
        : null;
    const fields = Object.fromEntries(
        Array.from(form.querySelectorAll("[data-account-field]"))
            .map(input => [input.dataset.accountField, input])
    );
    const touched = new Set();
    let passwordBlurred = false;

    const messageFor = input => {
        const fieldName = input.getAttribute("name");
        return fieldName
            ? form.querySelector(`[data-valmsg-for="${CSS.escape(fieldName)}"]`)
            : null;
    };

    const setFieldState = (input, state, message = "") => {
        if (!input) {
            return;
        }

        const messageElement = messageFor(input);
        input.classList.remove("is-invalid", "is-valid");
        input.removeAttribute("aria-invalid");

        if (messageElement) {
            messageElement.textContent = message;
            messageElement.classList.remove("field-validation-error", "field-validation-valid", "is-success", "is-hint");
            messageElement.classList.add(message && state !== "hint" ? "field-validation-error" : "field-validation-valid");
        }

        if (state === "error") {
            input.classList.add("is-invalid");
            input.setAttribute("aria-invalid", "true");
        } else if (state === "valid") {
            input.classList.add("is-valid");
            input.setAttribute("aria-invalid", "false");
            if (messageElement && message) {
                messageElement.classList.add("is-success");
            }
        } else if (state === "hint" && messageElement) {
            messageElement.classList.add("is-hint");
        }
    };

    const clearSummary = () => {
        if (!summary) {
            return;
        }

        summary.textContent = "";
        summary.classList.remove("is-visible", "validation-summary-errors");
        summary.classList.add("validation-summary-valid");
    };

    const showSummary = message => {
        if (!summary) {
            return;
        }

        summary.textContent = message;
        summary.classList.add("is-visible");
    };

    const validators = {
        firstName: value => /^[\p{L}]+(?:[ '’\-][\p{L}]+)*$/u.test(value),
        lastName: value => /^[\p{L}]+(?:[ '’\-][\p{L}]+)*$/u.test(value),
        username: value => /^[A-Za-z0-9]+$/.test(value),
        email: value => /^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$/.test(value)
    };

    const limits = {
        firstName: 100,
        lastName: 100,
        username: 100,
        loginIdentifier: 256
    };

    const validateSimpleField = (key, force = false) => {
        const input = fields[key];
        if (!input || (!force && !touched.has(key))) {
            return true;
        }

        const value = input.value;
        if (!value.trim()) {
            setFieldState(input, "error", input.dataset.requiredMessage || "");
            return false;
        }

        if (limits[key] && value.length > limits[key]) {
            setFieldState(input, "error", input.dataset.maxMessage || "");
            return false;
        }

        if (validators[key] && !validators[key](value)) {
            setFieldState(input, "error", input.dataset.invalidMessage || "");
            return false;
        }

        setFieldState(input, "valid");
        return true;
    };

    const passwordChecks = value => ({
        length: value.length >= 8 && value.length <= 100,
        upper: /[A-Z]/.test(value),
        lower: /[a-z]/.test(value),
        digit: /\d/.test(value)
    });

    const passwordIsValid = value => Object.values(passwordChecks(value)).every(Boolean);

    const renderPasswordRules = markInvalid => {
        const password = fields.password;
        const rules = form.querySelector("[data-password-rules]");
        if (!password || !rules) {
            return true;
        }

        const checks = passwordChecks(password.value);
        rules.querySelectorAll("[data-password-rule]").forEach(rule => {
            const isValid = checks[rule.dataset.passwordRule];
            const dot = rule.querySelector(".password-rule-dot");
            rule.classList.toggle("is-valid", isValid);
            rule.classList.toggle("is-invalid", markInvalid && !isValid);
            if (dot) {
                dot.textContent = isValid ? "✓" : (markInvalid ? "×" : "○");
            }
        });

        const isValid = passwordIsValid(password.value);
        password.classList.toggle("is-valid", password.value.length > 0 && isValid);
        password.classList.toggle("is-invalid", markInvalid && !isValid);
        return isValid;
    };

    const validateConfirmation = force => {
        const confirmation = fields.confirmPassword;
        if (!confirmation || (!force && !confirmation.value)) {
            return true;
        }

        if (!confirmation.value) {
            setFieldState(confirmation, "error", confirmation.dataset.requiredMessage || "");
            return false;
        }

        if (confirmation.value !== fields.password?.value) {
            setFieldState(confirmation, "error", confirmation.dataset.invalidMessage || "");
            return false;
        }

        setFieldState(confirmation, "valid", confirmation.dataset.validMessage || "");
        return true;
    };

    const simpleKeys = form.dataset.accountValidation === "login"
        ? ["loginIdentifier", "password"]
        : ["firstName", "lastName", "username", "email"];

    Object.entries(fields).forEach(([key, input]) => {
        if (messageFor(input)?.classList.contains("field-validation-error")) {
            touched.add(key);
            input.classList.add("is-invalid");
            input.setAttribute("aria-invalid", "true");
        }
    });

    simpleKeys.forEach(key => {
        const input = fields[key];
        if (!input) {
            return;
        }

        input.addEventListener("blur", () => {
            touched.add(key);
            validateSimpleField(key, true);
        });

        input.addEventListener("input", () => {
            clearSummary();
            if (touched.has(key)) {
                validateSimpleField(key, true);
            }
        });
    });

    if (fields.username) {
        fields.username.addEventListener("focus", () => {
            if (!touched.has("username") && !fields.username.value) {
                setFieldState(fields.username, "hint", fields.username.dataset.hintMessage || "");
            }
        });
    }

    const passwordRules = form.querySelector("[data-password-rules]");
    if (fields.password && passwordRules) {
        fields.password.addEventListener("focus", () => {
            passwordRules.classList.add("is-visible");
            renderPasswordRules(false);
        });
        fields.password.addEventListener("input", () => {
            passwordRules.classList.add("is-visible");
            renderPasswordRules(passwordBlurred);
            if (fields.confirmPassword?.value) {
                validateConfirmation(false);
            }
            clearSummary();
        });
        fields.password.addEventListener("blur", () => {
            passwordBlurred = true;
            renderPasswordRules(true);
        });
    }

    if (fields.confirmPassword) {
        fields.confirmPassword.addEventListener("input", () => {
            validateConfirmation(false);
            clearSummary();
        });
        fields.confirmPassword.addEventListener("blur", () => validateConfirmation(true));
    }

    form.addEventListener("submit", event => {
        simpleKeys.forEach(key => touched.add(key));
        const simpleFieldsValid = simpleKeys
            .map(key => validateSimpleField(key, true))
            .every(Boolean);

        let passwordValid = true;
        let confirmationValid = true;
        if (form.dataset.accountValidation === "register") {
            passwordBlurred = true;
            passwordRules?.classList.add("is-visible");
            passwordValid = renderPasswordRules(true);
            confirmationValid = validateConfirmation(true);
        }

        if (simpleFieldsValid && passwordValid && confirmationValid) {
            return;
        }

        event.preventDefault();
        showSummary(form.dataset.checkFieldsMessage || "");
        form.querySelector(".is-invalid")?.focus();
    });
})();
