// Fix for navigation dropdown and global functions
document.addEventListener('DOMContentLoaded', function () {
    // Fix dropdown toggle
    const dropdowns = document.querySelectorAll('.dropdown-toggle');
    dropdowns.forEach(dropdown => {
        dropdown.addEventListener('click', function (e) {
            e.preventDefault();
            const dropdownMenu = this.nextElementSibling;
            if (dropdownMenu) {
                dropdownMenu.classList.toggle('show');
            }
        });
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.matches('.dropdown-toggle')) {
            const dropdowns = document.querySelectorAll('.dropdown-menu');
            dropdowns.forEach(dropdown => {
                if (dropdown.classList.contains('show')) {
                    dropdown.classList.remove('show');
                }
            });
        }
    });

    // Fix export dropdown in navigation
    const exportOptions = document.querySelectorAll('.export-btn');
    exportOptions.forEach(option => {
        option.addEventListener('click', function (e) {
            e.preventDefault();
            const format = this.getAttribute('data-format');
            // Get current semester ID from somewhere or prompt
            const semesterId = prompt('Enter Semester ID for export:', '1');
            if (semesterId) {
                window.location.href = `/Registration/ExportRegistrations?semesterId=${semesterId}&format=${format}`;
            }
        });
    });
});

// Global functions for navigation
window.showPendingRegistrations = function () {
    window.location.href = '/Registration/Management?filter=pending';
};

window.showRegistrationStats = function () {
    window.location.href = '/Registration/Analytics';
};

window.showImportModal = function () {
    const modal = new bootstrap.Modal(document.getElementById('importModal'));
    modal.show();
};

window.showExportOptions = function () {
    alert('Export options would appear here');
};