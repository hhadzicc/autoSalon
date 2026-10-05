(() => {
    const drawer = document.getElementById("mobileNavigation");
    const mobileQuery = window.matchMedia("(max-width: 991.98px)");

    if (!drawer || !window.bootstrap) {
        return;
    }

    let startX = 0;
    let startY = 0;
    let startTime = 0;
    let currentX = 0;
    let isTracking = false;
    let isDragging = false;

    const getBackdrop = () => document.querySelector(".offcanvas-backdrop.show");

    const clearDragStyles = () => {
        drawer.classList.remove("is-dragging");
        drawer.style.removeProperty("transform");
        drawer.style.removeProperty("transition");

        const backdrop = getBackdrop();
        if (backdrop) {
            backdrop.style.removeProperty("opacity");
            backdrop.style.removeProperty("transition");
        }
    };

    drawer.addEventListener("touchstart", event => {
        if (!mobileQuery.matches || !drawer.classList.contains("show") || event.touches.length !== 1) {
            return;
        }

        const touch = event.touches[0];
        startX = touch.clientX;
        startY = touch.clientY;
        currentX = startX;
        startTime = performance.now();
        isTracking = true;
        isDragging = false;
    }, { passive: true });

    drawer.addEventListener("touchmove", event => {
        if (!isTracking || event.touches.length !== 1) {
            return;
        }

        const touch = event.touches[0];
        const deltaX = touch.clientX - startX;
        const deltaY = touch.clientY - startY;

        if (!isDragging) {
            if (Math.abs(deltaX) < 8 && Math.abs(deltaY) < 8) {
                return;
            }

            if (deltaX <= 0 || Math.abs(deltaX) <= Math.abs(deltaY) * 1.15) {
                isTracking = false;
                return;
            }

            isDragging = true;
            drawer.classList.add("is-dragging");
        }

        event.preventDefault();
        currentX = touch.clientX;

        const distance = Math.max(0, Math.min(deltaX, drawer.offsetWidth));
        const progress = distance / drawer.offsetWidth;
        drawer.style.transform = `translate3d(${distance}px, 0, 0)`;

        const backdrop = getBackdrop();
        if (backdrop) {
            backdrop.style.transition = "none";
            backdrop.style.opacity = String(0.5 * (1 - progress));
        }
    }, { passive: false });

    const finishGesture = () => {
        if (!isTracking || !isDragging) {
            isTracking = false;
            isDragging = false;
            return;
        }

        const distance = Math.max(0, currentX - startX);
        const elapsed = Math.max(1, performance.now() - startTime);
        const velocity = distance / elapsed;
        const shouldClose = distance >= drawer.offsetWidth * 0.25 || (distance >= 32 && velocity >= 0.55);
        const backdrop = getBackdrop();

        drawer.classList.remove("is-dragging");
        drawer.style.transition = "transform 180ms cubic-bezier(0.4, 0, 1, 1)";
        drawer.style.transform = shouldClose ? "translate3d(100%, 0, 0)" : "translate3d(0, 0, 0)";

        if (backdrop) {
            backdrop.style.transition = "opacity 180ms ease";
            backdrop.style.opacity = shouldClose ? "0" : "0.5";
        }

        window.setTimeout(() => {
            if (shouldClose) {
                drawer.style.transition = "none";
                bootstrap.Offcanvas.getOrCreateInstance(drawer).hide();
            } else {
                clearDragStyles();
            }
        }, 180);

        isTracking = false;
        isDragging = false;
    };

    drawer.addEventListener("touchend", finishGesture, { passive: true });
    drawer.addEventListener("touchcancel", finishGesture, { passive: true });
    drawer.addEventListener("hidden.bs.offcanvas", clearDragStyles);
})();
