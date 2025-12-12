// Create a file: wwwroot/js/dropdown-fix.js
document.addEventListener('DOMContentLoaded', function () {
    console.log('Fixing dropdowns...');

    // Fix for registration dropdown
    const regDropdown = document.querySelector('a[href="#registrationDropdown"]');
    if (regDropdown) {
        console.log('Found registration dropdown');
        regDropdown.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const dropdownMenu = this.nextElementSibling;
            if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                const isShown = dropdownMenu.classList.contains('show');
                dropdownMenu.classList.toggle('show');
                console.log('Dropdown toggled:', !isShown);
            }
        });
    }

    // Fix all Bootstrap dropdowns
    document.querySelectorAll('.dropdown-toggle').forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const dropdownMenu = this.nextElementSibling;
            if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                dropdownMenu.classList.toggle('show');
            }
        });
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
        }
    });
});

// Add to your _Layout.cshtml
// <script src="~/js/dropdown-fix.js" asp-append-version="true"></script>