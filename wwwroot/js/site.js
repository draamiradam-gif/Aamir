// Theme Management System
class ThemeManager {
    static init() {
        this.loadTheme();
        this.initThemeSwitcher();
        this.applyThemeTransition();
    }

    static loadTheme() {
        const savedTheme = localStorage.getItem('academic-theme') || 'dark';
        this.setTheme(savedTheme);
    }

    static setTheme(themeName) {
        document.documentElement.setAttribute('data-theme', themeName);
        localStorage.setItem('academic-theme', themeName);

        // Update active theme in switcher
        this.updateActiveTheme(themeName);

        // Dispatch theme change event
        document.dispatchEvent(new CustomEvent('themeChanged', { detail: themeName }));

        // Add transition class
        this.applyThemeTransition();

        // Show toast notification
        this.showThemeChangeToast(themeName);
    }

    static updateActiveTheme(themeName) {
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            option.classList.remove('active');
            if (option.dataset.theme === themeName) {
                option.classList.add('active');
            }
        });
    }

    static initThemeSwitcher() {
        // Only create switcher if it doesn't exist
        if (!document.getElementById('themeSwitcher')) {
            const themeSwitcherHTML = `
                <div class="theme-switcher" id="themeSwitcher">
                    <button class="theme-toggle-btn" id="themeToggle">
                        <i class="fas fa-palette"></i>
                    </button>
                    <div class="theme-panel">
                        <h6 class="mb-3">Theme Settings</h6>
                        <div class="theme-options">
                            <div class="theme-option active" data-theme="dark">
                                <div class="theme-preview" style="background: linear-gradient(135deg, #0f172a, #1e293b);"></div>
                                <div class="theme-name">Dark</div>
                            </div>
                            <div class="theme-option" data-theme="light">
                                <div class="theme-preview" style="background: linear-gradient(135deg, #ffffff, #f8fafc);"></div>
                                <div class="theme-name">Light</div>
                            </div>
                            <div class="theme-option" data-theme="blue">
                                <div class="theme-preview" style="background: linear-gradient(135deg, #0c4a6e, #0f172a);"></div>
                                <div class="theme-name">Ocean</div>
                            </div>
                            <div class="theme-option" data-theme="green">
                                <div class="theme-preview" style="background: linear-gradient(135deg, #064e3b, #0f172a);"></div>
                                <div class="theme-name">Forest</div>
                            </div>
                            <div class="theme-option" data-theme="purple">
                                <div class="theme-preview" style="background: linear-gradient(135deg, #581c87, #1e1b4b);"></div>
                                <div class="theme-name">Royal</div>
                            </div>
                        </div>
                        <div class="theme-actions">
                            <button class="btn btn-modern btn-sm w-100" id="resetTheme">
                                <i class="fas fa-sync me-2"></i>Reset to Default
                            </button>
                        </div>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', themeSwitcherHTML);
        }

        // Add event listeners
        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', (e) => {
                e.stopPropagation();
                this.toggleThemePanel();
            });
        }

        document.querySelectorAll('.theme-option').forEach(option => {
            option.addEventListener('click', (e) => {
                e.stopPropagation();
                const theme = option.dataset.theme;
                this.setTheme(theme);
                this.hideThemePanel();
            });
        });

        const resetTheme = document.getElementById('resetTheme');
        if (resetTheme) {
            resetTheme.addEventListener('click', (e) => {
                e.stopPropagation();
                this.setTheme('dark');
                this.hideThemePanel();
            });
        }

        // Close panel when clicking outside
        document.addEventListener('click', () => {
            this.hideThemePanel();
        });

        // Prevent panel from closing when clicking inside
        const themePanel = document.querySelector('.theme-panel');
        if (themePanel) {
            themePanel.addEventListener('click', (e) => {
                e.stopPropagation();
            });
        }
    }

    static toggleThemePanel() {
        const themeSwitcher = document.getElementById('themeSwitcher');
        if (themeSwitcher) {
            themeSwitcher.classList.toggle('open');
        }
    }

    static hideThemePanel() {
        const themeSwitcher = document.getElementById('themeSwitcher');
        if (themeSwitcher) {
            themeSwitcher.classList.remove('open');
        }
    }

    static applyThemeTransition() {
        document.body.classList.add('theme-transition');
        setTimeout(() => {
            document.body.classList.remove('theme-transition');
        }, 500);
    }

    static showThemeChangeToast(themeName) {
        const themes = {
            'dark': 'Dark Mode',
            'light': 'Light Mode',
            'blue': 'Ocean Theme',
            'green': 'Forest Theme',
            'purple': 'Royal Theme'
        };

        const message = `Theme changed to ${themes[themeName] || themeName}`;
        showToast(message, 'success');
    }

    static getCurrentTheme() {
        return localStorage.getItem('academic-theme') || 'dark';
    }

    static cycleThemes() {
        const themes = ['dark', 'light', 'blue', 'green', 'purple'];
        const currentTheme = this.getCurrentTheme();
        const currentIndex = themes.indexOf(currentTheme);
        const nextIndex = (currentIndex + 1) % themes.length;
        this.setTheme(themes[nextIndex]);
    }
}


// Add to site.js after the class definitions
class MessageHandler {
    static init() {
        this.cleanupMessageListeners();
        this.setupSafeMessageHandling();
    }

    static cleanupMessageListeners() {
        // Get all message event listeners
        const originalAddEventListener = EventTarget.prototype.addEventListener;
        let messageListeners = [];

        // Track message listeners
        EventTarget.prototype.addEventListener = function (type, listener, options) {
            if (type === 'message') {
                console.warn('Message listener added:', { listener: listener.toString().substring(0, 100) });
                messageListeners.push({ target: this, listener, options });

                // Wrap the listener to prevent async response errors
                const wrappedListener = function (event) {
                    try {
                        // Don't return true from async listeners
                        const result = listener.call(this, event);

                        // If it's a promise, catch any errors
                        if (result && typeof result.then === 'function') {
                            result.catch(error => {
                                console.error('Message listener promise rejected:', error);
                            });
                        }

                        return result;
                    } catch (error) {
                        console.error('Message listener error:', error);
                        return false; // Don't indicate async response
                    }
                };

                return originalAddEventListener.call(this, type, wrappedListener, options);
            }
            return originalAddEventListener.call(this, type, listener, options);
        };
    }

    static setupSafeMessageHandling() {
        // Remove problematic postMessage usage
        const originalPostMessage = window.postMessage;

        // Only override if we detect problematic usage
        if (window.postMessage.toString().includes('native code')) {
            return; // Native implementation is fine
        }

        window.postMessage = function (message, targetOrigin, transfer) {
            try {
                // Ensure proper message format
                const safeMessage = typeof message === 'string' ?
                    { type: 'custom', data: message } :
                    message;

                return originalPostMessage.call(window, safeMessage, targetOrigin || '*', transfer);
            } catch (error) {
                console.error('postMessage error:', error);
                return false;
            }
        };
    }
}

// Toast notification function
function showToast(message, type = 'info') {
    // Remove existing toasts
    const existingToasts = document.querySelectorAll('.toast');
    existingToasts.forEach(toast => {
        if (toast.parentElement) {
            toast.remove();
        }
    });

    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <span class="toast-message">${message}</span>
            <button class="toast-close">×</button>
        </div>
    `;

    // Add styles if not exists
    if (!document.querySelector('#toast-styles')) {
        const styles = document.createElement('style');
        styles.id = 'toast-styles';
        styles.textContent = `
            .toast {
                position: fixed;
                top: 20px;
                right: 20px;
                background: #333;
                color: white;
                padding: 12px 16px;
                border-radius: 8px;
                z-index: 10000;
                max-width: 300px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.3);
                animation: slideIn 0.3s ease;
                backdrop-filter: blur(10px);
                border: 1px solid rgba(255,255,255,0.1);
            }
            .toast-success { background: rgba(16, 185, 129, 0.9); }
            .toast-error { background: rgba(239, 68, 68, 0.9); }
            .toast-warning { background: rgba(245, 158, 11, 0.9); }
            .toast-info { background: rgba(59, 130, 246, 0.9); }
            .toast-content {
                display: flex;
                justify-content: space-between;
                align-items: center;
            }
            .toast-close {
                background: none;
                border: none;
                color: inherit;
                font-size: 18px;
                cursor: pointer;
                margin-left: 10px;
                padding: 0 5px;
            }
            @keyframes slideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
        `;
        document.head.appendChild(styles);
    }

    // Add to page
    document.body.appendChild(toast);

    // Add close event
    const closeBtn = toast.querySelector('.toast-close');
    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            toast.remove();
        });
    }

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.remove();
        }
    }, 5000);
}

// Mobile Menu Functionality
class MobileMenu {
    static init() {
        this.setupMobileMenu();
        this.setupResizeHandler();
    }

    static setupMobileMenu() {
        const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
        const mobileSidebar = document.getElementById('mobileSidebar');
        const backdrop = document.getElementById('sidebarBackdrop');

        if (mobileMenuBtn && mobileSidebar && backdrop) {
            mobileMenuBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.toggleMobileMenu();
            });

            backdrop.addEventListener('click', () => {
                this.closeMobileMenu();
            });
        }
    }

    static toggleMobileMenu() {
        const mobileSidebar = document.getElementById('mobileSidebar');
        const backdrop = document.getElementById('sidebarBackdrop');

        if (mobileSidebar && backdrop) {
            const isOpen = mobileSidebar.classList.contains('mobile-open');

            if (isOpen) {
                this.closeMobileMenu();
            } else {
                this.openMobileMenu();
            }
        }
    }

    static openMobileMenu() {
        const mobileSidebar = document.getElementById('mobileSidebar');
        const backdrop = document.getElementById('sidebarBackdrop');

        if (mobileSidebar && backdrop) {
            mobileSidebar.classList.add('mobile-open');
            backdrop.classList.add('mobile-open');
        }
    }

    static closeMobileMenu() {
        const mobileSidebar = document.getElementById('mobileSidebar');
        const backdrop = document.getElementById('sidebarBackdrop');

        if (mobileSidebar && backdrop) {
            mobileSidebar.classList.remove('mobile-open');
            backdrop.classList.remove('mobile-open');
        }
    }

    static setupResizeHandler() {
        window.addEventListener('resize', () => {
            if (window.innerWidth > 768) {
                this.closeMobileMenu();
            }
        });
    }
}

// DataTables Helper
class DataTablesHelper {
    static init() {
        // Initialize DataTables for all tables with class 'dataTable'
        if (typeof $.fn.DataTable !== 'undefined') {
            $('table.dataTable').each(function () {
                const table = $(this);
                if (!$.fn.DataTable.isDataTable(table)) {
                    table.DataTable({
                        pageLength: 25,
                        responsive: true,
                        language: {
                            search: "_INPUT_",
                            searchPlaceholder: "Search..."
                        },
                        dom: '<"row"<"col-md-6"l><"col-md-6"f>>rt<"row"<"col-md-6"i><"col-md-6"p>>'
                    });
                }
            });
        } else {
            console.warn('DataTables library not loaded');
        }
    }
}

// Safe event listener helper
function safeAddEventListener(selector, event, handler) {
    const element = document.querySelector(selector);
    if (element) {
        element.addEventListener(event, handler);
    }
}
// Add to site.js before the DOMContentLoaded event
// Replace the problematic ModalManager.preventModalOverflow method
class ModalManager {
    static init() {
        this.setupModalCloseHandlers();
        this.preventModalOverflow();
    }

    static setupModalCloseHandlers() {
        // Close modal on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
        });

        // Close modal on backdrop click
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal-overlay')) {
                this.closeAllModals();
            }
        });
    }

    static closeAllModals() {
        document.querySelectorAll('.modal-overlay.active, .modal-container.active').forEach(el => {
            el.classList.remove('active');
        });

        // Re-enable body scrolling
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }

    static preventModalOverflow() {
        // Check if Bootstrap Modal exists
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            // Safe check for Bootstrap modals
            $(document).on('show.bs.modal', '.modal', function () {
                // Don't add modal-open class to body
                document.body.style.overflow = 'auto';
                document.body.style.paddingRight = '0';
            });

            $(document).on('hidden.bs.modal', '.modal', function () {
                // Clean up backdrop
                $('.modal-backdrop').remove();
                document.body.style.overflow = 'auto';
            });
        }
    }

    static openModal(modalId) {
        const overlay = document.querySelector('.modal-overlay');
        const modal = document.getElementById(modalId);

        if (overlay && modal) {
            // Close any open modals first
            this.closeAllModals();

            // Show new modal
            overlay.classList.add('active');
            modal.classList.add('active');

            // Prevent body scrolling
            document.body.style.overflow = 'hidden';
        }
    }

    static closeModal(modalId) {
        const overlay = document.querySelector('.modal-overlay');
        const modal = document.getElementById(modalId);

        if (overlay) overlay.classList.remove('active');
        if (modal) modal.classList.remove('active');

        document.body.style.overflow = '';
    }
}

// Initialize everything when DOM is loaded
// Update the DOMContentLoaded event listener
document.addEventListener('DOMContentLoaded', function () {
    console.log('Multi-Theme Enhanced Site.js initialization started');

    // Initialize message handler first
    MessageHandler.init();

    // Initialize theme system
    ThemeManager.init();

    // Initialize mobile menu
    MobileMenu.init();

    // Initialize modal manager
    ModalManager.init();

    // Initialize DataTables
    DataTablesHelper.init();

    // Add keyboard shortcut for theme cycling (Ctrl+Shift+T)
    document.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.shiftKey && e.key === 'T') {
            e.preventDefault();
            ThemeManager.cycleThemes();
        }
    });

    // Fix for Bootstrap modals if they exist
    if (typeof bootstrap !== 'undefined') {
        // Fix modal backdrop issues
        $(document).on('show.bs.modal', '.modal', function () {
            // Keep body scrollable
            $('body').css('overflow', 'auto');
        });

        $(document).on('hidden.bs.modal', '.modal', function () {
            // Clean up backdrop
            $('.modal-backdrop').remove();
            $('body').css('overflow', 'auto');
        });
    }

    console.log('Multi-Theme Enhanced Site.js initialization complete');
});

// Add to site.js
class LogoutHelper {
    static init() {
        this.setupLogoutHandlers();
        this.setupSessionTimeout();
    }

    static setupLogoutHandlers() {
        // Handle logout button clicks
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('logout-btn') ||
                e.target.closest('.logout-btn')) {
                e.preventDefault();
                this.performLogout();
            }
        });
    }

    static async performLogout() {
        try {
            // Create a form and submit it
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = '/Logout/Index';

            // Add anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                const tokenClone = token.cloneNode(true);
                form.appendChild(tokenClone);
            }

            document.body.appendChild(form);
            form.submit();
        } catch (error) {
            console.error('Logout error:', error);
            // Fallback: redirect to force logout
            window.location.href = '/Logout/ForceLogout';
        }
    }

    static setupSessionTimeout() {
        // Auto logout after 30 minutes of inactivity
        let timeout;

        const resetTimer = () => {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                this.performLogout();
            }, 30 * 60 * 1000); // 30 minutes
        };

        // Reset timer on user activity
        ['click', 'mousemove', 'keypress', 'scroll'].forEach(event => {
            window.addEventListener(event, resetTimer);
        });

        resetTimer();
    }
}

// Update DataTablesHelper.init() in site.js
class ModalManager {
    static init() {
        this.setupModalCloseHandlers();
        this.preventModalOverflow();
    }

    static setupModalCloseHandlers() {
        // Close modal on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
        });

        // Close modal on backdrop click
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal-overlay')) {
                this.closeAllModals();
            }
        });
    }

    static closeAllModals() {
        document.querySelectorAll('.modal-overlay.active, .modal-container.active').forEach(el => {
            el.classList.remove('active');
        });

        // Re-enable body scrolling
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }

    static preventModalOverflow() {
        // Simply fix body overflow for modals
        const observer = new MutationObserver(() => {
            if (document.body.classList.contains('modal-open')) {
                document.body.style.overflow = 'auto';
                document.body.style.paddingRight = '0';
            }
        });

        observer.observe(document.body, { attributes: true, attributeFilter: ['class'] });
    }

    static openModal(modalId) {
        const overlay = document.querySelector('.modal-overlay');
        const modal = document.getElementById(modalId);

        if (overlay && modal) {
            // Close any open modals first
            this.closeAllModals();

            // Show new modal
            overlay.classList.add('active');
            modal.classList.add('active');

            // Prevent body scrolling
            document.body.style.overflow = 'hidden';
        }
    }

    static closeModal(modalId) {
        const overlay = document.querySelector('.modal-overlay');
        const modal = document.getElementById(modalId);

        if (overlay) overlay.classList.remove('active');
        if (modal) modal.classList.remove('active');

        document.body.style.overflow = '';
    }
}


// Initialize in DOMContentLoaded
document.addEventListener('DOMContentLoaded', function () {
    LogoutHelper.init();
});