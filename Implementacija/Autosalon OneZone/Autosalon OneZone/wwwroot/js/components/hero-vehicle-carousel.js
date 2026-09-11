(() => {
    const carousels = document.querySelectorAll("[data-hero-vehicle-carousel]");

    carousels.forEach((carousel) => {
        const slides = Array.from(carousel.querySelectorAll("[data-hero-carousel-slide]"));
        const dots = Array.from(carousel.querySelectorAll("[data-hero-carousel-dot]"));
        const previousButton = carousel.querySelector("[data-hero-carousel-previous]");
        const nextButton = carousel.querySelector("[data-hero-carousel-next]");

        if (slides.length < 2) {
            return;
        }

        const autoplayDelay = Number(carousel.dataset.autoplayDelay) || 5000;
        const transitionDuration = Number(carousel.dataset.transitionDuration) || 650;
        let currentIndex = Math.max(0, slides.findIndex((slide) => slide.classList.contains("is-active")));
        let autoplayTimer = null;
        let transitionTimer = null;
        let isPaused = false;
        let isTransitioning = false;

        carousel.style.setProperty("--hero-carousel-transition", `${transitionDuration}ms`);

        const clearAutoplay = () => {
            if (autoplayTimer !== null) {
                window.clearTimeout(autoplayTimer);
                autoplayTimer = null;
            }
        };

        const scheduleAutoplay = () => {
            clearAutoplay();

            if (isPaused || document.hidden) {
                return;
            }

            autoplayTimer = window.setTimeout(() => {
                showSlide((currentIndex + 1) % slides.length, 1);
            }, autoplayDelay);
        };

        const updateDots = () => {
            dots.forEach((dot, index) => {
                const isCurrent = index === currentIndex;
                dot.classList.toggle("is-active", isCurrent);
                dot.setAttribute("aria-current", isCurrent ? "true" : "false");
            });
        };

        const showSlide = (nextIndex, direction) => {
            if (isTransitioning || nextIndex === currentIndex) {
                scheduleAutoplay();
                return;
            }

            clearAutoplay();
            isTransitioning = true;

            const currentSlide = slides[currentIndex];
            const nextSlide = slides[nextIndex];
            const enteringClass = direction < 0 ? "is-entering-left" : "is-entering-right";
            const leavingClass = direction < 0 ? "is-leaving-right" : "is-leaving-left";

            nextSlide.classList.remove(
                "is-active",
                "is-entering-left",
                "is-entering-right",
                "is-leaving-left",
                "is-leaving-right"
            );
            nextSlide.classList.add(enteringClass);
            nextSlide.setAttribute("aria-hidden", "false");

            void nextSlide.offsetWidth;

            currentSlide.classList.remove("is-active");
            currentSlide.classList.add(leavingClass);
            currentSlide.setAttribute("aria-hidden", "true");
            nextSlide.classList.remove(enteringClass);
            nextSlide.classList.add("is-active");

            currentIndex = nextIndex;
            updateDots();

            window.clearTimeout(transitionTimer);
            transitionTimer = window.setTimeout(() => {
                currentSlide.classList.remove(leavingClass);
                isTransitioning = false;
                scheduleAutoplay();
            }, transitionDuration);
        };

        previousButton?.addEventListener("click", () => {
            const previousIndex = (currentIndex - 1 + slides.length) % slides.length;
            showSlide(previousIndex, -1);
        });

        nextButton?.addEventListener("click", () => {
            showSlide((currentIndex + 1) % slides.length, 1);
        });

        dots.forEach((dot) => {
            dot.addEventListener("click", () => {
                const nextIndex = Number(dot.dataset.heroCarouselDot);
                showSlide(nextIndex, nextIndex < currentIndex ? -1 : 1);
            });
        });

        carousel.addEventListener("mouseenter", () => {
            isPaused = true;
            clearAutoplay();
        });

        carousel.addEventListener("mouseleave", () => {
            isPaused = false;
            scheduleAutoplay();
        });

        carousel.addEventListener("focusin", () => {
            isPaused = true;
            clearAutoplay();
        });

        carousel.addEventListener("focusout", (event) => {
            if (!carousel.contains(event.relatedTarget)) {
                isPaused = false;
                scheduleAutoplay();
            }
        });

        document.addEventListener("visibilitychange", scheduleAutoplay);
        window.addEventListener("pagehide", () => {
            clearAutoplay();
            window.clearTimeout(transitionTimer);
        });

        updateDots();
        scheduleAutoplay();
    });
})();
