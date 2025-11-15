// wwwroot/js/students.js
class StudentManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.updateActionButtons();
    }

    setupEventListeners() {
        const selectAll = document.getElementById('selectAll');
        const selectAllBtn = document.getElementById('selectAllBtn');
        const deselectAllBtn = document.getElementById('deselectAllBtn');

        if (selectAll) {
            selectAll.addEventListener('change', () => this.toggleAllCheckboxes());
        }

        if (selectAllBtn) {
            selectAllBtn.addEventListener('click', () => this.selectAll());
        }

        if (deselectAllBtn) {
            deselectAllBtn.addEventListener('click', () => this.deselectAll());
        }

        // Individual checkbox changes
        document.addEventListener('change', (e) => {
            if (e.target.classList.contains('student-checkbox')) {
                this.updateActionButtons();
                this.updateSelectAllCheckbox();
            }
        });
    }

    toggleAllCheckboxes() {
        const checkboxes = document.getElementsByClassName('student-checkbox');
        const selectAll = document.getElementById('selectAll');

        for (let checkbox of checkboxes) {
            checkbox.checked = selectAll.checked;
        }
        this.updateActionButtons();
    }

    selectAll() {
        const checkboxes = document.getElementsByClassName('student-checkbox');
        for (let checkbox of checkboxes) {
            checkbox.checked = true;
        }
        document.getElementById('selectAll').checked = true;
        this.updateActionButtons();
    }

    deselectAll() {
        const checkboxes = document.getElementsByClassName('student-checkbox');
        for (let checkbox of checkboxes) {
            checkbox.checked = false;
        }
        document.getElementById('selectAll').checked = false;
        this.updateActionButtons();
    }

    updateActionButtons() {
        const checkedBoxes = document.querySelectorAll('.student-checkbox:checked');
        const deleteBtn = document.getElementById('deleteSelectedBtn');
        const exportBtn = document.getElementById('exportSelectedBtn');

        if (deleteBtn && exportBtn) {
            const count = checkedBoxes.length;
            deleteBtn.disabled = count === 0;
            exportBtn.disabled = count === 0;

            deleteBtn.textContent = `Delete Selected (${count})`;
            exportBtn.textContent = `Export Selected (${count})`;
        }
    }

    updateSelectAllCheckbox() {
        const allCheckboxes = document.querySelectorAll('.student-checkbox');
        const checkedBoxes = document.querySelectorAll('.student-checkbox:checked');
        const selectAll = document.getElementById('selectAll');

        if (selectAll) {
            selectAll.checked = allCheckboxes.length === checkedBoxes.length;
            selectAll.indeterminate = checkedBoxes.length > 0 && checkedBoxes.length < allCheckboxes.length;
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    new StudentManager();
});