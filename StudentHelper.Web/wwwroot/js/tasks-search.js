/* Tasks Index Page - Search with Cursor Position */

document.addEventListener('DOMContentLoaded', function() {
    let timeout = null;
    const searchInput = document.getElementById("searchInput");

    if (searchInput) {
        const savedCursorPosition = sessionStorage.getItem("taskSearchCursorPosition");
        const shouldFocus = sessionStorage.getItem("taskSearchFocus");

        if (shouldFocus === "true") {
            searchInput.focus();

            if (savedCursorPosition !== null) {
                const position = parseInt(savedCursorPosition, 10);
                searchInput.setSelectionRange(position, position);
            }

            sessionStorage.removeItem("taskSearchFocus");
            sessionStorage.removeItem("taskSearchCursorPosition");
        }

        // Save cursor position before search
        searchInput.addEventListener('keyup', function() {
            clearTimeout(timeout);
            sessionStorage.setItem("taskSearchFocus", "true");
            sessionStorage.setItem("taskSearchCursorPosition", this.selectionStart);

            timeout = setTimeout(function() {
                // Auto-submit search if needed
            }, 300);
        });
    }
});
