// Form utilities and validation
(function() {
    'use strict';

    // Form validation
    window.validateForm = function(formSelector) {
        var form = document.querySelector(formSelector);
        if (!form) return true;

        var isValid = true;
        var inputs = form.querySelectorAll('[required]');

        Array.prototype.forEach.call(inputs, function(input) {
            if (!input.value.trim()) {
                input.classList.add('is-invalid');
                isValid = false;
            } else {
                input.classList.remove('is-invalid');
                if (validateInput(input)) {
                    input.classList.add('is-valid');
                }
            }
        });

        return isValid;
    };

    // Validate individual input
    function validateInput(input) {
        var type = input.type;
        var value = input.value.trim();

        switch (type) {
            case 'email':
                return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
            case 'url':
                // Заміна конструкції new URL() на просту регулярку для сумісності з ES5
                var urlPattern = /^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$/;
                return urlPattern.test(value);
            case 'number':
                return !isNaN(value) && value !== '';
            case 'date':
                return !isNaN(Date.parse(value));
            default:
                return true;
        }
    }

    // Clear form
    window.clearForm = function(formSelector) {
        var form = document.querySelector(formSelector);
        if (form) {
            form.reset();
            var elements = form.querySelectorAll('input, textarea, select');
            Array.prototype.forEach.call(elements, function(el) {
                el.classList.remove('is-invalid', 'is-valid');
            });
        }
    };

    // Add input change listeners for live validation
    document.addEventListener('input', function(e) {
        var input = e.target;
        if (input.hasAttribute('required')) {
            if (input.value.trim()) {
                input.classList.remove('is-invalid');
                if (validateInput(input)) {
                    input.classList.add('is-valid');
                } else {
                    input.classList.add('is-invalid');
                }
            } else {
                input.classList.remove('is-valid', 'is-invalid');
            }
        }
    });

    // Handle form submission with AJAX (переписано з async/await на Promises)
    window.submitFormAjax = function(formSelector, successCallback, errorCallback) {
        var form = document.querySelector(formSelector);
        if (!form) return;

        form.addEventListener('submit', function(e) {
            e.preventDefault();

            if (!window.validateForm(formSelector)) {
                window.showNotification('Будь ласка, заповніть всі необхідні поля', 'danger');
                return;
            }

            var formData = new FormData(form);
            var submitBtn = form.querySelector('button[type="submit"]');

            if (window.setButtonLoading) {
                window.setButtonLoading(submitBtn, true);
            }

            // Використовуємо стандартні Promises замість async/await
            fetch(form.action, {
                method: form.method || 'POST',
                body: formData
            })
            .then(function(response) {
                if (response.ok) {
                    window.showNotification('Операція успішна', 'success');
                    if (successCallback) successCallback(response);
                } else {
                    response.text().then(function(errorText) {
                        window.showNotification(errorText || 'Помилка при обробці', 'danger');
                    });
                    if (errorCallback) errorCallback(response);
                }
            })
            .catch(function(error) {
                window.showNotification('Помилка підключення: ' + error.message, 'danger');
                if (errorCallback) errorCallback(error);
            })
            .then(function() {
                // Аналог блоку 'finally'
                if (window.setButtonLoading) {
                    window.setButtonLoading(submitBtn, false);
                }
            });
        });
    };

    // Initialize form handlers
    document.addEventListener('DOMContentLoaded', function() {
        // Add asterisks to required fields
        var requiredInputs = document.querySelectorAll('input[required], textarea[required], select[required]');
        Array.prototype.forEach.call(requiredInputs, function(input) {
            var label = document.querySelector('label[for="' + input.id + '"]');
            if (label && !label.classList.contains('required')) {
                label.classList.add('required');
            }
        });
    });

    console.log('StudentHelper forms.js initialized');
})();