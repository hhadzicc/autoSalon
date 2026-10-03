(() => {
    const form = document.querySelector("[data-support-reply-form]");
    if (!form) return;

    const message = form.querySelector("#support-message");
    const error = form.querySelector("#support-message-error");

    const setInvalid = (invalid) => {
        message.classList.toggle("is-invalid", invalid);
        message.toggleAttribute("aria-invalid", invalid);
        error.classList.toggle("d-none", !invalid);
        error.classList.toggle("d-block", invalid);
    };

    form.addEventListener("submit", (event) => {
        const length = message.value.trim().length;
        if (length >= 10 && length <= 4000) {
            setInvalid(false);
            return;
        }

        event.preventDefault();
        setInvalid(true);
        message.focus();
    });

    message.addEventListener("input", () => {
        const length = message.value.trim().length;
        if (length >= 10 && length <= 4000) setInvalid(false);
    });
})();
