// ============================================
// ENHANCED SITE.JS WITH SAFE EVENT HANDLING
// ============================================

// Safe event listener helper
function safeAddEventListener(selector, event, handler) {
    const elements = document.querySelectorAll(selector);
    elements.forEach(element => {
        if (element) {
            // Remove existing listener first to avoid duplicates
            element.removeEventListener(event, handler);
            element.addEventListener(event, handler);
        }
    });
}

// Initialize all event listeners safely
function initializeEventListeners() {
    console.log('🔧 Initializing event listeners...');

    // Theme Switcher
    safeAddEventListener('#themeToggle', 'click', function (e) {
        e.stopPropagation();
        if (window.ThemeManager) {
            window.ThemeManager.toggleThemePanel();
        }
    });

    safeAddEventListener('.theme-option', 'click', function (e) {
        e.stopPropagation();
        const theme = this.dataset.theme;
        if (window.ThemeManager) {
            window.ThemeManager.setTheme(theme);
            window.ThemeManager.hideThemePanel();
        }
    });

    safeAddEventListener('#resetTheme', 'click', function (e) {
        e.stopPropagation();
        if (window.ThemeManager) {
            window.ThemeManager.setTheme('dark');
            window.ThemeManager.hideThemePanel();
        }
    });

    // Mobile Menu
    safeAddEventListener('.mobile-menu-btn', 'click', function (e) {
        e.stopPropagation();
        if (window.MobileMenu) {
            window.MobileMenu.toggleMobileMenu();
        }
    });

    safeAddEventListener('#sidebarBackdrop', 'click', function () {
        if (window.MobileMenu) {
            window.MobileMenu.closeMobileMenu();
        }
    });

    // Close theme panel when clicking outside
    document.addEventListener('click', function () {
        if (window.ThemeManager) {
            window.ThemeManager.hideThemePanel();
        }
    });

    // Prevent panel from closing when clicking inside
    const themePanel = document.querySelector('.theme-panel');
    if (themePanel) {
        themePanel.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
}

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
        this.updateActiveTheme(themeName);
        this.applyThemeTransition();
        this.showThemeChangeToast(themeName);
    }

    static updateActiveTheme(themeName) {
        document.querySelectorAll('.theme-option').forEach(option => {
            option.classList.toggle('active', option.dataset.theme === themeName);
        });
    }

    static initThemeSwitcher() {
        // Theme switcher HTML is already in your layout
        console.log('🎨 Theme switcher initialized');
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
        if (window.showToast) {
            showToast(message, 'success');
        }
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

// Message Handler
class MessageHandler {
    static init() {
        this.setupSafeMessageHandling();
    }

    static setupSafeMessageHandling() {
        // Safe message handling
        console.log('📨 Message handler initialized');
    }
}

// Mobile Menu
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

// Modal Manager
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
    }

    static closeAllModals() {
        // Close bootstrap modals
        if (typeof bootstrap !== 'undefined') {
            document.querySelectorAll('.modal').forEach(modal => {
                const bsModal = bootstrap.Modal.getInstance(modal);
                if (bsModal) {
                    bsModal.hide();
                }
            });
        }

        // Close safe windows
        if (window.SafeWindows) {
            window.SafeWindows.closeAll();
        }

        // Ensure body is unlocked
        document.body.style.overflow = 'auto';
        document.body.classList.remove('modal-open');
    }

    static preventModalOverflow() {
        // Fix for Bootstrap modals
        if (typeof bootstrap !== 'undefined') {
            $(document).on('show.bs.modal', '.modal', function () {
                document.body.style.overflow = 'auto';
            });

            $(document).on('hidden.bs.modal', '.modal', function () {
                $('.modal-backdrop').remove();
                document.body.style.overflow = 'auto';
                document.body.classList.remove('modal-open');
            });
        }
    }
}

// DataTables Helper
// DataTables Helper - FIXED VERSION
class DataTablesHelper {
    static init() {
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
                        dom: '<"row"<"col-md-6"l><"col-md-6"f>>rt<"row"<"col-md-6"i><"col-md-6"p>>',
                        destroy: true, // Add this to prevent reinitialization
                        retrieve: true // Add this to retrieve existing instance
                    });
                }
            });
        }
    }
}

// Logout Helper
class LogoutHelper {
    static init() {
        this.setupLogoutHandlers();
    }

    static setupLogoutHandlers() {
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('logout-btn') || e.target.closest('.logout-btn')) {
                e.preventDefault();
                this.performLogout();
            }
        });
    }

    static async performLogout() {
        try {
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = '/Logout/Index';

            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                const tokenClone = token.cloneNode(true);
                form.appendChild(tokenClone);
            }

            document.body.appendChild(form);
            form.submit();
        } catch (error) {
            console.error('Logout error:', error);
            window.location.href = '/Logout/ForceLogout';
        }
    }
}

// Toast notification function
function showToast(message, type = 'info') {
    // Remove existing toasts
    document.querySelectorAll('.toast').forEach(toast => toast.remove());

    // Create toast
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
            .toast-content { display: flex; justify-content: space-between; align-items: center; }
            .toast-close { background: none; border: none; color: inherit; font-size: 18px; cursor: pointer; margin-left: 10px; padding: 0 5px; }
            @keyframes slideIn { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
        `;
        document.head.appendChild(styles);
    }

    // Add to page
    document.body.appendChild(toast);

    // Add close event
    toast.querySelector('.toast-close')?.addEventListener('click', () => toast.remove());

    // Auto remove
    setTimeout(() => toast.remove(), 5000);
}

// ============================================
// MAIN INITIALIZATION
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Multi-Theme Enhanced Site.js initialization started');

    // Force unlock page first
    if (window.SafeWindows) {
        window.SafeWindows.unlockPage();
    } else {
        // Emergency unlock
        document.body.style.overflow = 'auto';
        document.body.classList.remove('modal-open');
        document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
    }

    // Initialize message handler
    MessageHandler.init();

    // Initialize theme system
    ThemeManager.init();

    // Initialize event listeners
    initializeEventListeners();

    // Initialize mobile menu
    MobileMenu.init();

    // Initialize modal manager
    ModalManager.init();

    // Initialize DataTables
    DataTablesHelper.init();

    // Initialize logout helper
    LogoutHelper.init();

    // Initialize collapsible sidebar sections
    if (typeof SidebarManager !== 'undefined') {
        // Already initialized in sidebar-collapsible.js
        console.log('✅ Sidebar sections are collapsible');
    }
    // Add keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl+Shift+T for theme cycling
        if (e.ctrlKey && e.shiftKey && e.key === 'T') {
            e.preventDefault();
            ThemeManager.cycleThemes();
        }

        // Ctrl+W to close all windows
        if (e.ctrlKey && e.key === 'w') {
            e.preventDefault();
            if (window.SafeWindows) {
                window.SafeWindows.closeAll();
            }
        }
    });

    // Fix for Bootstrap modals
    if (typeof bootstrap !== 'undefined') {
        $(document).on('show.bs.modal', '.modal', function () {
            $('body').css('overflow', 'auto');
        });

        $(document).on('hidden.bs.modal', '.modal', function () {
            $('.modal-backdrop').remove();
            $('body').css('overflow', 'auto');
        });
    }

    // Fix dropdowns in site.js
    function fixDropdowns() {
        console.log('🔧 Fixing dropdowns...');

        // Registration dropdown specific fix
        const regDropdown = document.querySelector('#registrationDropdown');
        if (regDropdown) {
            // Remove and re-add the element
            const parent = regDropdown.parentElement;
            const clone = regDropdown.cloneNode(true);
            regDropdown.remove();
            parent.insertBefore(clone, parent.firstChild);

            // Initialize Bootstrap
            if (typeof bootstrap !== 'undefined') {
                new bootstrap.Dropdown(clone);
            }

            console.log('✅ Registration dropdown fixed');
        }

        // Fix all sidebar dropdowns
        document.querySelectorAll('.sidebar-container .dropdown').forEach(dropdown => {
            dropdown.addEventListener('show.bs.dropdown', function () {
                console.log('Dropdown opening:', this);
            });

            dropdown.addEventListener('hide.bs.dropdown', function () {
                console.log('Dropdown closing:', this);
            });
        });
    }

    // Call it on page load
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(fixDropdowns, 500); // Delay to ensure Bootstrap is loaded
    });

    // Add keyboard shortcut for dropdown fix
    document.addEventListener('keydown', function (e) {
        if (e.ctrlKey && e.shiftKey && e.key === 'D') {
            e.preventDefault();
            fixDropdowns();
            showToast('Dropdowns fixed', 'success');
        }
    });

    console.log('✅ Multi-Theme Enhanced Site.js initialization complete');
});

// Global exports
window.ThemeManager = ThemeManager;
window.MobileMenu = MobileMenu;
window.ModalManager = ModalManager;
window.showToast = showToast;