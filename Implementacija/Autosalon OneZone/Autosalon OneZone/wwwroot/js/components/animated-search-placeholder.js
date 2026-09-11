(() => {
    const typeSpeed = 80;
    const deleteSpeed = 40;
    const pauseAfterTyping = 5500;
    const pauseBeforeNext = 500;

    const inputs = document.querySelectorAll("[data-animated-search-placeholder]");
    inputs.forEach(input => {
        const staticPlaceholder = input.dataset.staticPlaceholder || "";
        const phrases = (input.dataset.placeholderPhrases || "")
            .split("|")
            .map(phrase => phrase.trim())
            .filter(Boolean);

        input.placeholder = staticPlaceholder;

        if (phrases.length === 0) {
            return;
        }

        let phraseIndex = 0;
        let characterIndex = 0;
        let deleting = false;
        let timerId = null;

        const clearTimer = () => {
            if (timerId === null) return;
            window.clearTimeout(timerId);
            timerId = null;
        };

        const schedule = (callback, delay) => {
            clearTimer();
            timerId = window.setTimeout(callback, delay);
        };

        const startPlaceholderAnimation = (delay = pauseBeforeNext) => {
            clearTimer();
            characterIndex = 0;
            deleting = false;

            if (input.value || document.activeElement === input || document.hidden) {
                input.placeholder = staticPlaceholder;
                return;
            }

            input.placeholder = staticPlaceholder;
            schedule(animate, delay);
        };

        const animate = () => {
            timerId = null;

            if (input.value || document.activeElement === input || document.hidden) {
                input.placeholder = staticPlaceholder;
                return;
            }

            const phrase = phrases[phraseIndex];

            if (!deleting) {
                characterIndex += 1;
                input.placeholder = phrase.slice(0, characterIndex);

                if (characterIndex >= phrase.length) {
                    deleting = true;
                    schedule(animate, pauseAfterTyping);
                    return;
                }

                schedule(animate, typeSpeed);
                return;
            }

            characterIndex -= 1;
            input.placeholder = phrase.slice(0, characterIndex);

            if (characterIndex <= 0) {
                deleting = false;
                phraseIndex = (phraseIndex + 1) % phrases.length;
                schedule(animate, pauseBeforeNext);
                return;
            }

            schedule(animate, deleteSpeed);
        };

        input.addEventListener("focus", () => {
            clearTimer();
            input.placeholder = staticPlaceholder;
        });

        input.addEventListener("input", () => {
            if (input.value) clearTimer();
            input.placeholder = staticPlaceholder;
        });

        input.addEventListener("blur", () => {
            if (input.value) return;
            startPlaceholderAnimation();
        });

        document.addEventListener("visibilitychange", () => {
            clearTimer();
            if (document.hidden || input.value || document.activeElement === input) {
                input.placeholder = staticPlaceholder;
                return;
            }

            startPlaceholderAnimation();
        });

        window.addEventListener("pagehide", clearTimer, { once: true });

        if (input.value) {
            input.placeholder = staticPlaceholder;
        } else {
            startPlaceholderAnimation();
        }
    });
})();
