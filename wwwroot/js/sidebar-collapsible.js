// Sidebar collapsible sections with dropdown fixes
document.addEventListener('DOMContentLoaded', function () {
    console.log('🔧 Initializing collapsible sidebar sections...');

    // Convert sections to collapsible
    convertSectionsToCollapsible();

    // Load collapsed state from localStorage
    loadCollapsedState();

    // Fix all dropdowns
    fixAllDropdowns();

    // Set up keyboard shortcuts
    setupKeyboardShortcuts();

    console.log('✅ Sidebar collapsible sections initialized');
});

function convertSectionsToCollapsible() {
    const sections = document.querySelectorAll('.section-header');

    sections.forEach(section => {
        // Skip if already converted
        if (section.classList.contains('collapsible-header')) return;

        // Get the section content
        const sectionContent = [];
        let nextElement = section.nextElementSibling;

        while (nextElement && !nextElement.classList.contains('section-header')) {
            sectionContent.push(nextElement);
            nextElement = nextElement.nextElementSibling;
        }

        if (sectionContent.length > 0) {
            // Create wrapper
            const wrapper = document.createElement('div');
            wrapper.className = 'collapsible-section';

            // Make section clickable
            section.classList.add('collapsible-header');
            section.innerHTML = `
                <span class="section-title">${section.textContent}</span>
                <span class="section-toggle">
                    <i class="fas fa-chevron-down"></i>
                </span>
            `;

            // Create content container
            const contentContainer = document.createElement('div');
            contentContainer.className = 'collapsible-content';

            // Move content into container
            sectionContent.forEach(item => {
                contentContainer.appendChild(item);
            });

            // Insert wrapper
            section.parentNode.insertBefore(wrapper, section);
            wrapper.appendChild(section);
            wrapper.appendChild(contentContainer);

            // Add click handler
            section.addEventListener('click', function (e) {
                e.stopPropagation();
                toggleSection(this);
            });

            // Set initial state
            const sectionTitle = section.querySelector('.section-title').textContent;
            if (sectionTitle.includes('DASHBOARD')) {
                expandSection(section);
            } else {
                collapseSection(section);
            }
        }
    });
}

function toggleSection(sectionHeader) {
    const content = sectionHeader.nextElementSibling;
    const isCollapsed = content.classList.contains('collapsed');

    if (isCollapsed) {
        expandSection(sectionHeader);
    } else {
        collapseSection(sectionHeader);
    }

    // Save state
    saveCollapsedState();
}

function expandSection(sectionHeader) {
    const content = sectionHeader.nextElementSibling;
    const toggleIcon = sectionHeader.querySelector('.section-toggle i');

    // Calculate content height
    content.style.maxHeight = content.scrollHeight + 'px';
    content.style.opacity = '1';
    content.classList.remove('collapsed');

    // Update icon
    toggleIcon.className = 'fas fa-chevron-down';

    // Update ARIA
    sectionHeader.setAttribute('aria-expanded', 'true');

    // Close other sections if this is a main section
    if (sectionHeader.querySelector('.section-title').textContent.includes('DASHBOARD')) {
        closeOtherSections(sectionHeader);
    }
}

function collapseSection(sectionHeader) {
    const content = sectionHeader.nextElementSibling;
    const toggleIcon = sectionHeader.querySelector('.section-toggle i');

    content.style.maxHeight = '0';
    content.style.opacity = '0';
    content.classList.add('collapsed');

    // Update icon
    toggleIcon.className = 'fas fa-chevron-right';

    // Update ARIA
    sectionHeader.setAttribute('aria-expanded', 'false');
}

function closeOtherSections(currentSection) {
    const allSections = document.querySelectorAll('.collapsible-header');
    allSections.forEach(section => {
        if (section !== currentSection) {
            collapseSection(section);
        }
    });
}

function loadCollapsedState() {
    const savedState = localStorage.getItem('sidebarCollapsedState');
    if (savedState) {
        try {
            const state = JSON.parse(savedState);
            const sections = document.querySelectorAll('.collapsible-header');

            sections.forEach(section => {
                const title = section.querySelector('.section-title').textContent;
                if (state[title] === false) {
                    expandSection(section);
                } else if (state[title] === true) {
                    collapseSection(section);
                }
            });
        } catch (e) {
            console.error('Error loading sidebar state:', e);
        }
    }
}

function saveCollapsedState() {
    const state = {};
    const sections = document.querySelectorAll('.collapsible-header');

    sections.forEach(section => {
        const title = section.querySelector('.section-title').textContent;
        const content = section.nextElementSibling;
        state[title] = content.classList.contains('collapsed');
    });

    localStorage.setItem('sidebarCollapsedState', JSON.stringify(state));
}

function fixAllDropdowns() {
    // Fix for registration dropdown
    const registrationToggle = document.querySelector('#registrationDropdown');
    if (registrationToggle) {
        const dropdownMenu = registrationToggle.nextElementSibling;

        // Position dropdown correctly when shown
        registrationToggle.addEventListener('show.bs.dropdown', function () {
            setTimeout(() => {
                if (dropdownMenu.classList.contains('show')) {
                    const rect = registrationToggle.getBoundingClientRect();
                    dropdownMenu.style.position = 'fixed';
                    dropdownMenu.style.left = (rect.left + rect.width - 10) + 'px';
                    dropdownMenu.style.top = (rect.top - 10) + 'px';
                }
            }, 10);
        });

        // Clean up when hidden
        registrationToggle.addEventListener('hide.bs.dropdown', function () {
            dropdownMenu.removeAttribute('style');
        });
    }

    // Fix all Bootstrap dropdowns in sidebar
    const sidebarDropdowns = document.querySelectorAll('.sidebar-container .dropdown-toggle');
    sidebarDropdowns.forEach(toggle => {
        const dropdownMenu = toggle.nextElementSibling;

        if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
            // Position dropdown correctly
            toggle.addEventListener('show.bs.dropdown', function () {
                setTimeout(() => {
                    if (dropdownMenu.classList.contains('show')) {
                        const rect = toggle.getBoundingClientRect();
                        dropdownMenu.style.position = 'fixed';
                        dropdownMenu.style.left = (rect.left + rect.width - 10) + 'px';
                        dropdownMenu.style.top = (rect.top - 10) + 'px';
                        dropdownMenu.style.zIndex = '1060';
                    }
                }, 10);
            });

            // Clean up when hidden
            toggle.addEventListener('hide.bs.dropdown', function () {
                dropdownMenu.removeAttribute('style');
            });
        }
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
                menu.removeAttribute('style');
            });
        }
    });

    // Close dropdowns when clicking on dropdown items (for non-Bootstrap links)
    document.querySelectorAll('.dropdown-item[href^="#"], .dropdown-item:not([href])').forEach(item => {
        item.addEventListener('click', function () {
            const dropdownMenu = this.closest('.dropdown-menu');
            if (dropdownMenu) {
                dropdownMenu.classList.remove('show');
                dropdownMenu.removeAttribute('style');
            }
        });
    });
}

function setupKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
        // Alt+S to toggle all sections
        if (e.altKey && e.key === 's') {
            e.preventDefault();
            toggleAllSections();
        }

        // Alt+D to expand all sections
        if (e.altKey && e.key === 'd') {
            e.preventDefault();
            expandAllSections();
        }

        // Alt+C to collapse all sections except Dashboard
        if (e.altKey && e.key === 'c') {
            e.preventDefault();
            collapseAllSections();
        }
    });
}

function toggleAllSections() {
    const sections = document.querySelectorAll('.collapsible-header');
    const allCollapsed = Array.from(sections).every(section =>
        section.nextElementSibling.classList.contains('collapsed')
    );

    sections.forEach(section => {
        if (allCollapsed) {
            expandSection(section);
        } else {
            collapseSection(section);
        }
    });

    saveCollapsedState();
}

function expandAllSections() {
    const sections = document.querySelectorAll('.collapsible-header');
    sections.forEach(section => {
        expandSection(section);
    });

    saveCollapsedState();
}

function collapseAllSections() {
    const sections = document.querySelectorAll('.collapsible-header');
    sections.forEach(section => {
        const title = section.querySelector('.section-title').textContent;
        if (!title.includes('DASHBOARD')) {
            collapseSection(section);
        }
    });

    saveCollapsedState();
}

// Export functions for global use
window.SidebarManager = {
    toggleSection: toggleSection,
    expandAllSections: expandAllSections,
    collapseAllSections: collapseAllSections,
    toggleAllSections: toggleAllSections,
    fixAllDropdowns: fixAllDropdowns
};