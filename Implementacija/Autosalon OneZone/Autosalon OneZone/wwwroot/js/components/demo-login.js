(() => {
    const form = document.getElementById("loginForm");
    if (!form) {
        return;
    }

    const loginIdentifier = form.querySelector('[data-account-field="loginIdentifier"]');
    const password = form.querySelector('[data-account-field="password"]');
    const submitButton = form.querySelector(".login-submit");
    const options = Array.from(form.querySelectorAll("[data-demo-login]"));
    if (!loginIdentifier || !password || options.length === 0) {
        return;
    }

    const updateSelection = selectedOption => {
        options.forEach(option => {
            const isSelected = option === selectedOption;
            option.classList.toggle("is-selected", isSelected);
            option.setAttribute("aria-pressed", String(isSelected));
        });
    };

    options.forEach(option => {
        option.addEventListener("click", () => {
            loginIdentifier.value = option.dataset.loginIdentifier || "";
            password.value = option.dataset.loginPassword || "";
            loginIdentifier.dispatchEvent(new Event("input", { bubbles: true }));
            password.dispatchEvent(new Event("input", { bubbles: true }));
            updateSelection(option);
            if (submitButton) {
                submitButton.classList.remove("is-demo-ready");
                void submitButton.offsetWidth;
                submitButton.classList.add("is-demo-ready");
            }
        });
    });

    submitButton?.addEventListener("animationend", () => {
        submitButton.classList.remove("is-demo-ready");
    });

    const syncSelection = () => {
        const selectedOption = options.find(option =>
            option.dataset.loginIdentifier === loginIdentifier.value &&
            option.dataset.loginPassword === password.value
        );
        updateSelection(selectedOption);
    };

    loginIdentifier.addEventListener("input", syncSelection);
    password.addEventListener("input", syncSelection);

    const defaultOption = options[0];
    if (defaultOption &&
        loginIdentifier.value === defaultOption.dataset.loginIdentifier &&
        !password.value) {
        password.value = defaultOption.dataset.loginPassword || "";
        password.dispatchEvent(new Event("input", { bubbles: true }));
    }

    syncSelection();
})();
