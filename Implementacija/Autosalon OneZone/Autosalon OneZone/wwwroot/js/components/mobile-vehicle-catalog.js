(function () {
    'use strict';

    var actions = document.querySelector('[data-mobile-catalog-actions]');
    var filterSheet = document.querySelector('[data-mobile-filter-sheet]');
    var sortSheet = document.querySelector('[data-mobile-sort-sheet]');
    var backdrop = document.querySelector('[data-mobile-catalog-backdrop]');
    var catalogNavbar = document.querySelector('.vehicle-catalog-navbar');
    var navbarSearch = document.querySelector('.vehicle-navbar-search');
    var navbarSearchInput = navbarSearch?.querySelector('input');
    var navbarSearchPlaceholder = navbarSearch?.querySelector('[data-mobile-search-placeholder]');

    if (!actions || !filterSheet || !sortSheet || !backdrop) {
        return;
    }

    var mobileQuery = window.matchMedia('(max-width: 720px)');
    var lastScrollY = window.scrollY;
    var activeSheet = null;

    function initializeMobileHeader() {
        if (!mobileQuery.matches || !catalogNavbar || !navbarSearchInput || !navbarSearchPlaceholder) {
            return;
        }

        var fullText = navbarSearchPlaceholder.dataset.text || '';
        var hasPlayed = sessionStorage.getItem('onezoneVehicleHeaderIntro') === '1';
        var shouldAnimate = !hasPlayed && window.location.search === '';

        if (!hasPlayed) {
            sessionStorage.setItem('onezoneVehicleHeaderIntro', '1');
        }

        navbarSearch.classList.toggle('has-value', Boolean(navbarSearchInput.value));
        navbarSearchInput.addEventListener('input', function () {
            navbarSearch.classList.toggle('has-value', Boolean(navbarSearchInput.value));
        });

        if (!shouldAnimate || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            catalogNavbar.classList.add('is-brand-collapsed');
            navbarSearchPlaceholder.textContent = fullText;
            return;
        }

        navbarSearchPlaceholder.textContent = '';

        window.setTimeout(function () {
            catalogNavbar.classList.add('is-brand-collapsed', 'is-brand-absorbing');
            window.setTimeout(function () {
                catalogNavbar.classList.remove('is-brand-absorbing');
            }, 900);
        }, 350);

        var characterIndex = 0;
        function typeNextCharacter() {
            if (navbarSearchInput.value || document.activeElement === navbarSearchInput) {
                navbarSearchPlaceholder.textContent = '';
                return;
            }

            characterIndex += 1;
            navbarSearchPlaceholder.textContent = fullText.slice(0, characterIndex);
            if (characterIndex < fullText.length) {
                window.setTimeout(typeNextCharacter, 105);
            }
        }

        window.setTimeout(typeNextCharacter, 250);

    }

    initializeMobileHeader();

    function closeSheet() {
        if (activeSheet) {
            activeSheet.classList.remove('is-open');
        }

        activeSheet = null;
        backdrop.hidden = true;
        document.body.classList.remove('vehicles-sheet-open');
        actions.classList.remove('is-hidden');
    }

    function openSheet(sheet) {
        if (!mobileQuery.matches) {
            return;
        }

        closeSheet();
        activeSheet = sheet;
        sheet.classList.add('is-open');
        backdrop.hidden = false;
        document.body.classList.add('vehicles-sheet-open');
        actions.classList.add('is-hidden');
    }

    document.querySelector('[data-mobile-filter-open]')?.addEventListener('click', function () {
        openSheet(filterSheet);
    });

    document.querySelector('[data-mobile-sort-open]')?.addEventListener('click', function () {
        openSheet(sortSheet);
    });

    document.querySelectorAll('[data-mobile-sheet-close]').forEach(function (button) {
        button.addEventListener('click', closeSheet);
    });

    backdrop.addEventListener('click', closeSheet);

    document.querySelectorAll('[data-mobile-sort-value]').forEach(function (button) {
        button.addEventListener('click', function () {
            var sortSelect = document.getElementById('sortOrder');
            var filterForm = document.getElementById('filterForm');

            if (!sortSelect || !filterForm) {
                return;
            }

            sortSelect.value = button.dataset.mobileSortValue || '';
            filterForm.submit();
        });
    });

    window.addEventListener('scroll', function () {
        if (!mobileQuery.matches || activeSheet) {
            return;
        }

        var currentScrollY = window.scrollY;
        var difference = currentScrollY - lastScrollY;

        if (currentScrollY <= 12 || difference < -4) {
            actions.classList.remove('is-hidden');
        } else if (difference > 4) {
            actions.classList.add('is-hidden');
        }

        lastScrollY = currentScrollY;
    }, { passive: true });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeSheet();
        }
    });

    mobileQuery.addEventListener('change', function (event) {
        if (!event.matches) {
            closeSheet();
        }
    });
})();
