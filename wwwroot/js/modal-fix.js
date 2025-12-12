// emergency-modal-fix.js
(function () {
    'use strict';

    console.log('Modal Emergency Fix Loading...');

    // 1. Immediately fix body
    document.body.style.overflow = 'auto';
    document.body.style.position = 'static';
    document.body.classList.remove('modal-open');

    // 2. Remove any blocking overlays
    function removeBlockers() {
        // Find and remove/modify elements that block the page
        const elements = document.querySelectorAll('*');
        elements.forEach(el => {
            const style = window.getComputedStyle(el);

            // Criteria for blocking elements
            const isBlocking = (
                // Full screen elements
                (style.position === 'fixed' &&
                    (style.width === '100%' || style.width === '100vw') &&
                    (style.height === '100%' || style.height === '100vh')) ||

                // High z-index overlays
                (parseInt(style.zIndex) > 1000 &&
                    (style.backgroundColor !== 'transparent' ||
                        style.backdropFilter !== 'none')) ||

                // Modal backdrops
                el.classList.contains('modal-backdrop') ||
                el.classList.contains('modal-overlay') ||
                el.getAttribute('id')?.includes('backdrop') ||
                el.getAttribute('id')?.includes('overlay')
            );

            if (isBlocking) {
                console.log('Removing blocker:', el);
                el.style.display = 'none';
                el.style.pointerEvents = 'none';
                el.style.backgroundColor = 'transparent';
                el.remove();
            }
        });
    }

    // 3. Fix modal events
    function fixModalEvents() {
        // Prevent default modal behaviors
        const originalShow = Element.prototype.showModal;
        const originalClose = Element.prototype.close;

        if (originalShow) {
            Element.prototype.showModal = function () {
                console.log('Modal show intercepted');
                this.style.display = 'block';
                this.style.zIndex = '1000';
                document.body.style.overflow = 'auto';
                return true;
            };
        }

        if (originalClose) {
            Element.prototype.close = function () {
                console.log('Modal close intercepted');
                this.style.display = 'none';
                document.body.style.overflow = 'auto';
                return true;
            };
        }
    }

    // 4. Make everything clickable
    function enableAllClicks() {
        document.addEventListener('click', function (e) {
            // Allow all clicks to propagate
            e.stopPropagation = function () {
                console.warn('Click propagation stopped, allowing anyway');
                // Don't actually stop propagation
            };
        }, true);

        document.addEventListener('mousedown', function (e) {
            e.stopPropagation = function () {
                // Don't stop propagation
            };
        }, true);
    }

    // 5. Fix floating windows specifically
    function fixFloatingWindows() {
        const floaters = document.querySelectorAll('.floating-window, [class*="float"], [class*="window"]');
        floaters.forEach(floater => {
            floater.style.position = 'absolute';
            floater.style.zIndex = '1000';
            floater.style.pointerEvents = 'auto';

            // Add close button if not exists
            if (!floater.querySelector('.close-float')) {
                const closeBtn = document.createElement('button');
                closeBtn.className = 'close-float';
                closeBtn.innerHTML = '×';
                closeBtn.style.cssText = `
                    position: absolute;
                    top: 10px;
                    right: 10px;
                    background: #ef4444;
                    color: white;
                    border: none;
                    border-radius: 50%;
                    width: 30px;
                    height: 30px;
                    cursor: pointer;
                    z-index: 1001;
                `;
                closeBtn.onclick = function () {
                    floater.style.display = 'none';
                };
                floater.appendChild(closeBtn);
            }
        });
    }

    // 6. Bootstrap modal fix
    function fixBootstrapModals() {
        if (typeof bootstrap !== 'undefined') {
            console.log('Fixing Bootstrap modals');

            // Override Modal constructor
            const OriginalModal = bootstrap.Modal;

            bootstrap.Modal = function (element, config) {
                const modal = new OriginalModal(element, config);

                // Override show method
                const originalShow = modal.show;
                modal.show = function () {
                    console.log('Bootstrap modal show intercepted');
                    document.body.style.overflow = 'auto';
                    document.body.classList.remove('modal-open');
                    originalShow.call(this);
                    // Remove backdrop immediately
                    setTimeout(() => {
                        const backdrops = document.querySelectorAll('.modal-backdrop');
                        backdrops.forEach(b => b.remove());
                    }, 10);
                };

                // Override hide method
                const originalHide = modal.hide;
                modal.hide = function () {
                    console.log('Bootstrap modal hide intercepted');
                    originalHide.call(this);
                    document.body.style.overflow = 'auto';
                    document.body.classList.remove('modal-open');
                };

                return modal;
            };
        }
    }

    // 7. Execute all fixes
    function applyAllFixes() {
        console.log('Applying emergency fixes...');

        // Immediate fixes
        removeBlockers();
        enableAllClicks();

        // DOM fixes
        setTimeout(() => {
            fixModalEvents();
            fixFloatingWindows();
            fixBootstrapModals();

            // Force remove any remaining blockers
            const blockers = document.querySelectorAll('body > div');
            blockers.forEach(div => {
                const style = window.getComputedStyle(div);
                if (style.position === 'fixed' &&
                    style.top === '0px' &&
                    style.left === '0px') {
                    console.log('Removing fixed blocker:', div);
                    div.remove();
                }
            });
        }, 100);

        // Periodic cleanup
        setInterval(removeBlockers, 5000);
    }

    // Run when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyAllFixes);
    } else {
        applyAllFixes();
    }

    // Also run on window load
    window.addEventListener('load', applyAllFixes);

    // Export for manual execution
    window.EmergencyModalFix = {
        applyAllFixes,
        removeBlockers,
        fixFloatingWindows
    };

    console.log('Modal Emergency Fix Loaded');
})();