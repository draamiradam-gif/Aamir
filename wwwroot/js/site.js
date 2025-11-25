// Site.js - Global JavaScript functions
console.log('Site.js loaded successfully');

// Global utility functions
function confirmDelete(message = 'Are you sure you want to delete this item?') {
    return confirm(message);
}

function confirmAction(message) {
    return confirm(message);
}

function showLoading() {
    document.body.style.cursor = 'wait';
    const loadingElements = document.querySelectorAll('.loading-spinner');
    loadingElements.forEach(el => el.style.display = 'inline-block');
}

function hideLoading() {
    document.body.style.cursor = 'default';
    const loadingElements = document.querySelectorAll('.loading-spinner');
    loadingElements.forEach(el => el.style.display = 'none');
}

// Form validation helpers
function validateRequiredFields(formId) {
    const form = document.getElementById(formId);
    if (!form) return true;

    const requiredFields = form.querySelectorAll('[required]');
    let isValid = true;

    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            field.classList.add('is-invalid');
            isValid = false;
        } else {
            field.classList.remove('is-invalid');
        }
    });

    return isValid;
}

// AJAX helper functions
function ajaxRequest(url, options = {}) {
    const defaultOptions = {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        }
    };

    const mergedOptions = { ...defaultOptions, ...options };

    if (mergedOptions.data && mergedOptions.method !== 'GET') {
        mergedOptions.body = JSON.stringify(mergedOptions.data);
    }

    return fetch(url, mergedOptions)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .catch(error => {
            console.error('AJAX request failed:', error);
            throw error;
        });
}

// Toast notification system
function showToast(message, type = 'info', duration = 5000) {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    // Show toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, { delay: duration });
    toast.show();

    // Remove toast from DOM after it's hidden
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

// Table utilities
function initializeDataTable(tableId, options = {}) {
    const table = document.getElementById(tableId);
    if (!table) return null;

    const defaultOptions = {
        responsive: true,
        language: {
            search: "_INPUT_",
            searchPlaceholder: "Search...",
            lengthMenu: "_MENU_ records per page",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "Showing 0 to 0 of 0 entries",
            infoFiltered: "(filtered from _MAX_ total entries)",
            zeroRecords: "No matching records found",
            paginate: {
                first: "First",
                last: "Last",
                next: "Next",
                previous: "Previous"
            }
        }
    };

    return $(table).DataTable({ ...defaultOptions, ...options });
}

// Date utilities
function formatDate(dateString) {
    if (!dateString) return '';

    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function formatDateTime(dateString) {
    if (!dateString) return '';

    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// File utilities
function validateFileInput(input, allowedTypes = [], maxSize = 5 * 1024 * 1024) {
    if (!input.files || input.files.length === 0) {
        return { isValid: false, message: 'Please select a file' };
    }

    const file = input.files[0];

    // Check file size
    if (file.size > maxSize) {
        const maxSizeMB = maxSize / (1024 * 1024);
        return {
            isValid: false,
            message: `File size must be less than ${maxSizeMB}MB`
        };
    }

    // Check file type
    if (allowedTypes.length > 0 && !allowedTypes.includes(file.type)) {
        return {
            isValid: false,
            message: `File type not allowed. Allowed types: ${allowedTypes.join(', ')}`
        };
    }

    return { isValid: true, message: 'File is valid' };
}

// Local storage utilities
function setLocalStorage(key, value) {
    try {
        localStorage.setItem(key, JSON.stringify(value));
        return true;
    } catch (error) {
        console.error('Error saving to localStorage:', error);
        return false;
    }
}

function getLocalStorage(key, defaultValue = null) {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : defaultValue;
    } catch (error) {
        console.error('Error reading from localStorage:', error);
        return defaultValue;
    }
}

function removeLocalStorage(key) {
    try {
        localStorage.removeItem(key);
        return true;
    } catch (error) {
        console.error('Error removing from localStorage:', error);
        return false;
    }
}

// URL utilities
function getQueryParam(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(name);
}

function updateQueryParam(key, value) {
    const url = new URL(window.location);
    if (value === null || value === '') {
        url.searchParams.delete(key);
    } else {
        url.searchParams.set(key, value);
    }
    window.history.replaceState({}, '', url);
}

// DOM ready function for global initialization
document.addEventListener('DOMContentLoaded', function () {
    // Auto-dismiss alerts
    const autoDismissAlerts = document.querySelectorAll('.alert[data-auto-dismiss]');
    autoDismissAlerts.forEach(alert => {
        const delay = parseInt(alert.getAttribute('data-auto-dismiss')) || 5000;
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, delay);
    });

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Add loading states to buttons with async actions
    document.addEventListener('submit', function (e) {
        const form = e.target;
        const submitButton = form.querySelector('button[type="submit"], input[type="submit"]');

        if (submitButton) {
            const originalText = submitButton.innerHTML;
            submitButton.innerHTML = '<span class="loading-spinner"></span> Processing...';
            submitButton.disabled = true;

            // Revert button state if form submission fails
            setTimeout(() => {
                if (!form.checkValidity()) {
                    submitButton.innerHTML = originalText;
                    submitButton.disabled = false;
                }
            }, 1000);
        }
    });

    // Global error handler for AJAX requests
    $(document).ajaxError(function (event, jqXHR, ajaxSettings, thrownError) {
        console.error('AJAX Error:', thrownError);
        showToast('An error occurred while processing your request.', 'danger');
    });

    console.log('Site.js initialization complete');
});

// Export functions for module usage (if needed)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        confirmDelete,
        showLoading,
        hideLoading,
        showToast,
        formatDate,
        formatDateTime
    };
}