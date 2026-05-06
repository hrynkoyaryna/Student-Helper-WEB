/* Tasks Edit - Character Counter */

document.addEventListener('DOMContentLoaded', function() {
    const textarea = document.getElementById('descriptionInput');
    const charCount = document.getElementById('charCount');
    
    if (textarea && charCount) {
        function updateCharCount() {
            const currentLength = textarea.value.length;
            charCount.textContent = currentLength + ' / 500 символів';
        }

        // Update on load
        updateCharCount();

        // Update on input
        textarea.addEventListener('input', updateCharCount);
    }
});
