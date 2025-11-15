// wwwroot/js/student-management.js
class StudentManagement {
    constructor() {
        this.baseUrl = window.location.origin;
        this.init();
    }

    init() {
        this.setupSearch();
        this.setupBulkActions();
    }

    setupSearch() {
        const searchForm = document.querySelector('form[method="get"]');
        const searchInput = searchForm?.querySelector('input[name="searchString"]');

        if (searchInput) {
            // Real-time search debounce
            let timeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(timeout);
                timeout = setTimeout(() => {
                    searchForm.submit();
                }, 500);
            });
        }
    }

    setupBulkActions() {
        const bulkForm = document.getElementById('bulkActionForm');

        if (bulkForm) {
            bulkForm.addEventListener('submit', (e) => {
                const checkedBoxes = document.querySelectorAll('.student-checkbox:checked');
                if (checkedBoxes.length === 0) {
                    e.preventDefault();
                    this.showToast('Please select at least one student', 'warning');
                }
            });
        }
    }

    async deleteStudent(studentId) {
        if (confirm('Are you sure you want to delete this student?')) {
            try {
                const response = await fetch(`${this.baseUrl}/Students/Delete/${studentId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

                if (response.ok) {
                    this.showToast('Student deleted successfully', 'success');
                    setTimeout(() => window.location.reload(), 1000);
                } else {
                    this.showToast('Error deleting student', 'error');
                }
            } catch (error) {
                this.showToast('Network error occurred', 'error');
            }
        }
    }

    showToast(message, type = 'info') {
        // Simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 1050; min-width: 300px;';
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.remove();
        }, 5000);
    }
}

// Initialize student management
document.addEventListener('DOMContentLoaded', function () {
    window.studentManager = new StudentManagement();
});