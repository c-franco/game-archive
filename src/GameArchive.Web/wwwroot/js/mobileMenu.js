// mobileMenu.js – GameArchive
// Handles the off-canvas sidebar on mobile (≤ 768px).
// Uses MutationObserver to survive Blazor WASM hot-navigation DOM rebuilds.

(function () {
    'use strict';

    var MOBILE_BP = 768;

    // ── State ─────────────────────────────────────────────────────────────────
    var sidebar   = null;
    var backdrop  = null;
    var header    = null;
    var hamburger = null;
    var initialized = false;

    // ── Helpers ───────────────────────────────────────────────────────────────

    function isMobile() {
        return window.innerWidth <= MOBILE_BP;
    }

    function openSidebar() {
        if (!sidebar) return;
        sidebar.classList.add('open');
        if (backdrop)  backdrop.classList.add('visible');
        if (hamburger) {
            hamburger.classList.add('open');
            hamburger.setAttribute('aria-expanded', 'true');
        }
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        if (!sidebar) return;
        sidebar.classList.remove('open');
        if (backdrop)  backdrop.classList.remove('visible');
        if (hamburger) {
            hamburger.classList.remove('open');
            hamburger.setAttribute('aria-expanded', 'false');
        }
        document.body.style.overflow = '';
    }

    function toggleSidebar() {
        if (sidebar && sidebar.classList.contains('open')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }

    // ── Build persistent elements (created once, never removed) ──────────────

    function ensureBackdrop() {
        if (backdrop && document.body.contains(backdrop)) return;
        backdrop = document.createElement('div');
        backdrop.className = 'sidebar-backdrop';
        backdrop.addEventListener('click', closeSidebar);
        document.body.appendChild(backdrop);
    }

    function ensureMobileHeader() {
        // Reuse if already in DOM
        var existing = document.querySelector('.mobile-header');
        if (existing) {
            header    = existing;
            hamburger = existing.querySelector('.hamburger');
            return;
        }

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
        brand.innerHTML = '<span class="mobile-header-icon">&#9672;</span><span>GameArchive</span>';

        header.appendChild(hamburger);
        header.appendChild(brand);

        // Insert as very first child of body so it stays on top
        document.body.insertBefore(header, document.body.firstChild);
    }

    // ── Wire up the sidebar found in the DOM ──────────────────────────────────

    function wireSidebar(el) {
        sidebar = el;

        // Close when a nav-link is tapped (Blazor SPA navigation)
        el.addEventListener('click', function (e) {
            var link = e.target.closest('.nav-link');
            if (link && isMobile()) closeSidebar();
        });
    }

    // ── Main init / re-init ───────────────────────────────────────────────────

    function setup() {
        var el = document.querySelector('.sidebar');
        if (!el) return;

        ensureBackdrop();
        ensureMobileHeader();
        wireSidebar(el);

        if (!initialized) {
            // One-time global listeners
            window.addEventListener('resize', function () {
                if (!isMobile()) closeSidebar();
            });
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') closeSidebar();
            });
            initialized = true;
        }
    }

    // ── MutationObserver: re-run setup whenever Blazor rebuilds the DOM ───────

    var observer = new MutationObserver(function () {
        var el = document.querySelector('.sidebar');
        if (el && el !== sidebar) {
            // Sidebar was (re)mounted by Blazor – re-wire
            closeSidebar();
            sidebar = null;
            setup();
        }
        // Also re-inject header if Blazor wiped it
        if (!document.querySelector('.mobile-header') && initialized) {
            ensureMobileHeader();
        }
    });

    // ── Bootstrap ─────────────────────────────────────────────────────────────

    function bootstrap() {
        setup();

        // Watch for Blazor DOM mutations
        observer.observe(document.body, { childList: true, subtree: true });

        // Fallback polling for slow Blazor first-load
        if (!sidebar) {
            var attempts = 0;
            var timer = setInterval(function () {
                attempts++;
                if (document.querySelector('.sidebar')) {
                    clearInterval(timer);
                    setup();
                } else if (attempts > 60) {
                    clearInterval(timer);
                }
            }, 150);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }

})();
