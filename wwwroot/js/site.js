// Basic JavaScript to resolve 404 errors
console.log('Site.js loaded');

function confirmDelete() {
    return confirm('Are you sure you want to delete this item?');
}

function showLoading() {
    document.body.style.cursor = 'wait';
}

function hideLoading() {
    document.body.style.cursor = 'default';
}