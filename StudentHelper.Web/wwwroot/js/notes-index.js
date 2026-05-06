/* Notes Index - Pin/Unpin Functionality */

document.addEventListener('DOMContentLoaded', function() {
    // Get antiforgery token from the hidden input
    const antiForgeryInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryToken = antiForgeryInput ? antiForgeryInput.value : '';

    // Pin buttons
    document.querySelectorAll('.pin-btn').forEach(btn => {
        btn.addEventListener('click', async function() {
            const noteId = this.getAttribute('data-note-id');
            try {
                const response = await fetch(`/Notes/Pin?id=${noteId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': antiForgeryToken,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });
                if (response.ok) {
                    location.reload();
                }
            } catch (err) {
                console.error('Error pinning note:', err);
            }
        });
    });

    // Unpin buttons
    document.querySelectorAll('.unpin-btn').forEach(btn => {
        btn.addEventListener('click', async function() {
            const noteId = this.getAttribute('data-note-id');
            try {
                const response = await fetch(`/Notes/Unpin?id=${noteId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': antiForgeryToken,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });
                if (response.ok) {
                    location.reload();
                }
            } catch (err) {
                console.error('Error unpinning note:', err);
            }
        });
    });
});
