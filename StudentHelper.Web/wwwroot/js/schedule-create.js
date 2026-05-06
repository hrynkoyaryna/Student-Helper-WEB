/* Schedule Create Page - Inline Scripts */

document.addEventListener('DOMContentLoaded', function() {
    // Check for success message and redirect
    const successAlert = document.querySelector('.alert-success');
    if (successAlert && successAlert.textContent.includes('успіш')) {
        setTimeout(function() {
            window.location = window.location.origin + '/Calendar';
        }, 1200);
    }
});
