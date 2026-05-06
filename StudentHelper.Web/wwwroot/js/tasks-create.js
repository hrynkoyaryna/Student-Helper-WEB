/* Tasks Create Page - Character Counter */

document.addEventListener('DOMContentLoaded', function() {
    const textarea = document.getElementById('descriptionInput');
    const charCount = document.getElementById('charCount');
    
    if (textarea && charCount) {
        function updateCharCount() {
            charCount.textContent = textarea.value.length;
        }

        // Update on load
        updateCharCount();

        // Update on input
        textarea.addEventListener('input', updateCharCount);
    }
});
