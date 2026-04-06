// mobileMenu.js
// Handles the off-canvas sidebar on mobile (≤ 768px).
// Injects a fixed top bar with a hamburger button and wires up the backdrop.

(function () {
    'use strict';

    var MOBILE_BP = 768;
    var sidebar, backdrop, header, hamburger;

    function isMobile() {
        return window.innerWidth <= MOBILE_BP;
    }

    function openSidebar() {
        sidebar.classList.add('open');
        backdrop.classList.add('visible');
        hamburger.classList.add('open');
        hamburger.setAttribute('aria-expanded', 'true');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        sidebar.classList.remove('open');
        backdrop.classList.remove('visible');
        hamburger.classList.remove('open');
        hamburger.setAttribute('aria-expanded', 'false');
        document.body.style.overflow = '';
    }

    function toggleSidebar() {
        if (sidebar.classList.contains('open')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }

    function buildMobileHeader() {
        header = document.createElement('div');
        header.className = 'mobile-header';

        hamburger = document.createElement('button');
        hamburger.className = 'hamburger';
        hamburger.setAttribute('aria-label', 'Abrir menú');
        hamburger.setAttribute('aria-expanded', 'false');
        hamburger.innerHTML = '<span></span><span></span><span></span>';
        hamburger.addEventListener('click', toggleSidebar);

        var brand = document.createElement('div');
        brand.className = 'mobile-header-brand';
        brand.innerHTML = '<span class="mobile-header-icon">◈</span><span>GameArchive</span>';

        header.appendChild(hamburger);
        header.appendChild(brand);

        document.body.insertBefore(header, document.body.firstChild);
    }

    function buildBackdrop() {
        backdrop = document.createElement('div');
        backdrop.className = 'sidebar-backdrop';
        backdrop.addEventListener('click', closeSidebar);
        document.body.appendChild(backdrop);
    }

    function init() {
        sidebar = document.querySelector('.sidebar');
        if (!sidebar) return;

        buildBackdrop();
        buildMobileHeader();

        // Close sidebar when a nav link is clicked (SPA navigation)
        sidebar.addEventListener('click', function (e) {
            var link = e.target.closest('.nav-link');
            if (link && isMobile()) {
                closeSidebar();
            }
        });

        // Close on resize back to desktop
        window.addEventListener('resize', function () {
            if (!isMobile() && sidebar.classList.contains('open')) {
                closeSidebar();
            }
        });

        // Close on Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeSidebar();
        });
    }

    // Wait for Blazor to render the app shell
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        // Blazor WASM renders asynchronously – poll until .sidebar appears
        var attempts = 0;
        var timer = setInterval(function () {
            attempts++;
            if (document.querySelector('.sidebar')) {
                clearInterval(timer);
                init();
            } else if (attempts > 60) {
                clearInterval(timer);
            }
        }, 200);
    }
})();
