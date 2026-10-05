(() => {
    const mobileQuery = window.matchMedia("(max-width: 575.98px)");
    const track = document.querySelector("[data-mobile-featured-carousel]");
    const dots = Array.from(document.querySelectorAll("[data-mobile-featured-dot]"));

    if (!track || dots.length < 2) {
        return;
    }

    const slides = Array.from(track.querySelectorAll(".home-featured-vehicle-item"));
    const autoplayDelay = Number(track.dataset.autoplayDelay) || 5000;
    let currentIndex = 0;
    let autoplayTimer = null;
    let scrollTimer = null;
    let isInteracting = false;

    const updateDots = index => {
        currentIndex = index;
        dots.forEach((dot, dotIndex) => {
            const isCurrent = dotIndex === currentIndex;
            dot.classList.toggle("is-active", isCurrent);
            dot.setAttribute("aria-current", isCurrent ? "true" : "false");
        });
    };

    const clearAutoplay = () => {
        window.clearTimeout(autoplayTimer);
        autoplayTimer = null;
    };

    const goToSlide = index => {
        const slide = slides[index];
        if (!slide) {
            return;
        }

        track.scrollTo({
            left: slide.offsetLeft - track.offsetLeft,
            behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth"
        });
        updateDots(index);
    };

    const scheduleAutoplay = () => {
        clearAutoplay();
        if (!mobileQuery.matches || isInteracting || document.hidden) {
            return;
        }

        autoplayTimer = window.setTimeout(() => {
            goToSlide((currentIndex + 1) % slides.length);
            scheduleAutoplay();
        }, autoplayDelay);
    };

    const syncCurrentSlide = () => {
        window.clearTimeout(scrollTimer);
        scrollTimer = window.setTimeout(() => {
            const nearestIndex = slides.reduce((bestIndex, slide, index) => {
                const bestDistance = Math.abs(slides[bestIndex].offsetLeft - track.offsetLeft - track.scrollLeft);
                const distance = Math.abs(slide.offsetLeft - track.offsetLeft - track.scrollLeft);
                return distance < bestDistance ? index : bestIndex;
            }, 0);
            updateDots(nearestIndex);
            scheduleAutoplay();
        }, 100);
    };

    dots.forEach((dot, index) => {
        dot.addEventListener("click", () => {
            goToSlide(index);
            scheduleAutoplay();
        });
    });

    track.addEventListener("scroll", syncCurrentSlide, { passive: true });
    track.addEventListener("touchstart", () => {
        isInteracting = true;
        clearAutoplay();
    }, { passive: true });
    track.addEventListener("touchend", () => {
        isInteracting = false;
        syncCurrentSlide();
    }, { passive: true });

    document.addEventListener("visibilitychange", scheduleAutoplay);
    mobileQuery.addEventListener("change", scheduleAutoplay);
    window.addEventListener("pagehide", clearAutoplay);

    updateDots(0);
    scheduleAutoplay();
})();
