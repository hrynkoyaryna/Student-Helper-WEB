// Schedule functionality
(function() {
    'use strict';

    window.Schedule = {
        data: [],
        container: null,

        init: function(containerId, data = []) {
            this.container = document.getElementById(containerId);
            if (!this.container) return;

            this.data = data;
            this.render();
            this.attachEventListeners();
        },

        render: function() {
            let html = '<table class="schedule-table"><thead><tr>';

            // Header with times
            const times = this.getScheduleTimes();
            html += '<th>Час</th>';
            times.forEach(time => {
                html += `<th>${time}</th>`;
            });
            html += '</tr></thead><tbody>';

            // Body rows
            this.data.forEach(row => {
                html += `<tr><td>${row.name}</td>`;
                row.items.forEach(item => {
                    if (item) {
                        html += `<td><div class="lesson-item" data-id="${item.id}">
                            <p class="lesson-name">${item.name}</p>
                            <div class="lesson-details">
                                <div class="lesson-teacher">${item.teacher || 'N/A'}</div>
                                <div class="lesson-room">${item.room || 'N/A'}</div>
                            </div>
                        </div></td>`;
                    } else {
                        html += '<td></td>';
                    }
                });
                html += '</tr>';
            });

            html += '</tbody></table>';
            this.container.innerHTML = html;
        },

        getScheduleTimes: function() {
            const times = [];
            for (let i = 8; i < 18; i++) {
                times.push(`${i}:00 - ${i + 1}:00`);
            }
            return times;
        },

        attachEventListeners: function() {
            this.container.querySelectorAll('.lesson-item').forEach(el => {
                el.addEventListener('click', (e) => {
                    const lessonId = el.dataset.id;
                    this.selectLesson(lessonId);
                });
            });
        },

        selectLesson: function(id) {
            this.container.querySelectorAll('.lesson-item').forEach(el => {
                el.classList.remove('selected');
            });

            const lessonEl = this.container.querySelector(`[data-id="${id}"]`);
            if (lessonEl) {
                lessonEl.classList.add('selected');
            }
        }
    };

    console.log('StudentHelper schedule.js initialized');
})();
