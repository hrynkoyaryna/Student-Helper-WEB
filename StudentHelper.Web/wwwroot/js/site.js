// Shared site utilities
(function() {
    'use strict';

    // Utility: Show notification
    window.showNotification = function(message, type, duration) {
        // Класична ініціалізація дефолтних значень
        var alertType = type || 'info';
        var alertDuration = duration === undefined ? 4000 : duration;

        var alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-' + alertType + ' alert-dismissible';
        alertDiv.innerHTML = 
            '<div class="alert-content">' + message + '</div>' +
            '<button type="button" class="alert-close" data-bs-dismiss="alert">&times;</button>';
        
        var container = document.querySelector('main') || document.body;
        container.insertAdjacentElement('afterbegin', alertDiv);
        
        if (alertDuration) {
            setTimeout(function() {
                alertDiv.remove();
            }, alertDuration);
        }
    };

    // Utility: Confirm dialog
    window.confirmAction = function(message) {
        return confirm(message);
    };

    // Utility: Format date
    window.formatDate = function(date, format) {
        var dateFormat = format || 'DD.MM.YYYY';
        var workingDate = date;

        if (typeof workingDate === 'string') {
            workingDate = new Date(workingDate);
        }
        
        var day = String(workingDate.getDate());
        if (day.length < 2) day = '0' + day;

        var month = String(workingDate.getMonth() + 1);
        if (month.length < 2) month = '0' + month;

        var year = workingDate.getFullYear();
        
        return dateFormat
            .replace('DD', day)
            .replace('MM', month)
            .replace('YYYY', year);
    };

    // Utility: Format time
    window.formatTime = function(date, format) {
        var timeFormat = format || 'HH:mm';
        var workingDate = date;

        if (typeof workingDate === 'string') {
            workingDate = new Date(workingDate);
        }
        
        var hours = String(workingDate.getHours());
        if (hours.length < 2) hours = '0' + hours;

        var minutes = String(workingDate.getMinutes());
        if (minutes.length < 2) minutes = '0' + minutes;
        
        return timeFormat
            .replace('HH', hours)
            .replace('mm', minutes);
    };

    // Utility: Debounce function (сумісна з ES5 за допомогою apply та arguments)
    window.debounce = function(func, wait) {
        var timeout;
        return function() {
            var context = this;
            var args = arguments;
            var later = function() {
                clearTimeout(timeout);
                func.apply(context, args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    // Utility: Throttle function
    window.throttle = function(func, limit) {
        var inThrottle;
        return function() {
            var context = this;
            var args = arguments;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(function() {
                    inThrottle = false;
                }, limit);
            }
        };
    };

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        document.addEventListener('DOMContentLoaded', function() {
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        });
    }

    // Smooth scrolling for anchor links
    document.addEventListener('click', function(e) {
        var link = e.target.closest('a[href^="#"]');
        if (link) {
            var target = document.querySelector(link.hash);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth' });
            }
        }
    });

    // Add loading state to buttons
    window.setButtonLoading = function(button, loading) {
        var isLoading = loading === undefined ? true : loading;
        if (isLoading) {
            button.disabled = true;
            button.setAttribute('data-original-text', button.textContent);
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Обробка...';
        } else {
            button.disabled = false;
            button.textContent = button.getAttribute('data-original-text') || button.textContent;
        }
    };

    // Log library loaded
    console.log('StudentHelper site.js initialized');
})();