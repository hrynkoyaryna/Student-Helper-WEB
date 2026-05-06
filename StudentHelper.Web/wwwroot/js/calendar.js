// Calendar functionality
(function() {
    'use strict';

    window.Calendar = {
        currentDate: new Date(),
        selectedDate: null,
        events: [],

        init: function(containerId, options) {
            this.container = document.getElementById(containerId);
            if (!this.container) return;

            // Класична альтернатива дефолтним значенням та оператору spread
            var opt = options || {};
            var defaultOptions = {
                onDateSelect: opt.onDateSelect || null,
                onMonthChange: opt.onMonthChange || null
            };

            // Об'єднуємо об'єкти без використання spread оператора (...)
            this.options = Object.assign ? Object.assign(defaultOptions, opt) : defaultOptions;

            this.render();
            this.attachEventListeners();
        },

        render: function() {
            var self = this; // Зберігаємо контекст для використання всередині анонімних функцій
            var year = this.currentDate.getFullYear();
            var month = this.currentDate.getMonth();

            var firstDay = new Date(year, month, 1).getDay();
            var daysInMonth = new Date(year, month + 1, 0).getDate();
            var daysInPrevMonth = new Date(year, month, 0).getDate();

            var html = '<div class="calendar-grid">';

            // Day headers
            var dayNames = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Нд'];
            dayNames.forEach(function(day) {
                html += '<div class="calendar-day-header">' + day + '</div>';
            });

            // Previous month days
            for (var i = firstDay - 1; i >= 0; i--) {
                var prevDay = daysInPrevMonth - i;
                html += '<div class="calendar-day other-month">' + prevDay + '</div>';
            }

            // Current month days
            for (var day = 1; day <= daysInMonth; day++) {
                var date = new Date(year, month, day);
                var today = new Date();
                var isToday = date.toDateString() === today.toDateString();
                
                // Заміна .some() зі стрілочною функцією на класичну анонімну функцію
                var hasEvent = this.events.some(function(e) {
                    return new Date(e.date).toDateString() === date.toDateString();
                });

                var classes = 'calendar-day';
                if (isToday) classes += ' today';
                if (hasEvent) classes += ' has-event';

                var dateStr = date.toISOString().split('T')[0];
                html += '<div class="' + classes + '" data-date="' + dateStr + '">' + day + '</div>';
            }

            // Next month days
            var totalCells = 42; // 6 weeks * 7 days
            var filledCells = firstDay + daysInMonth;
            for (var nextDay = 1; nextDay <= totalCells - filledCells; nextDay++) {
                html += '<div class="calendar-day other-month">' + nextDay + '</div>';
            }

            html += '</div>';
            this.container.innerHTML = html;
        },

        attachEventListeners: function() {
            var self = this; // Зберігаємо контекст
            var days = this.container.querySelectorAll('.calendar-day:not(.other-month)');
            
            // Використовуємо звичайний цикл або Array.prototype.forEach для безпеки
            Array.prototype.forEach.call(days, function(el) {
                el.addEventListener('click', function() {
                    var date = el.getAttribute('data-date');
                    self.selectDate(date);
                    if (self.options.onDateSelect) {
                        self.options.onDateSelect(new Date(date));
                    }
                });
            });
        },

        selectDate: function(date) {
            var days = this.container.querySelectorAll('.calendar-day');
            Array.prototype.forEach.call(days, function(el) {
                el.classList.remove('selected');
            });

            var dateEl = this.container.querySelector('[data-date="' + date + '"]');
            if (dateEl) {
                dateEl.classList.add('selected');
                this.selectedDate = new Date(date);
            }
        },

        nextMonth: function() {
            this.currentDate.setMonth(this.currentDate.getMonth() + 1);
            this.render();
            this.attachEventListeners();
            if (this.options.onMonthChange) {
                this.options.onMonthChange(this.currentDate);
            }
        },

        prevMonth: function() {
            this.currentDate.setMonth(this.currentDate.getMonth() - 1);
            this.render();
            this.attachEventListeners();
            if (this.options.onMonthChange) {
                this.options.onMonthChange(this.currentDate);
            }
        },

        addEvent: function(date, title, options) {
            var opt = options || {};
            var newEvent = {
                date: date,
                title: title
            };

            // Об'єднуємо об'єкти без spread оператора
            var mergedEvent = Object.assign ? Object.assign(newEvent, opt) : newEvent;
            this.events.push(mergedEvent);
        },

        removeEvent: function(date, title) {
            this.events = this.events.filter(function(e) {
                return !(e.date === date && e.title === title);
            });
        }
    };

    console.log('StudentHelper calendar.js initialized');
})();