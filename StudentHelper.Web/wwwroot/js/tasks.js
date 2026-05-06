// Tasks functionality
(function() {
    'use strict';

    window.Tasks = {
        tasks: [],
        container: null,
        filters: {
            status: 'all',
            priority: 'all'
        },

        init: function(containerId, tasks = []) {
            this.container = document.getElementById(containerId);
            if (!this.container) return;

            this.tasks = tasks;
            this.render();
            this.attachEventListeners();
        },

        render: function() {
            const filtered = this.getFilteredTasks();
            let html = '<ul class="task-list">';

            filtered.forEach(task => {
                const checked = task.completed ? 'checked' : '';
                const completedClass = task.completed ? 'completed' : '';
                
                html += `
                    <li class="task-item ${completedClass}" data-id="${task.id}">
                        <input type="checkbox" class="task-checkbox" ${checked}>
                        <div class="task-content">
                            <p class="task-title">${this.escapeHtml(task.title)}</p>
                            <div class="task-meta">
                                <span class="priority-badge priority-${task.priority}">${task.priority}</span>
                                <span class="task-date">${task.dueDate || 'Немає дати'}</span>
                            </div>
                        </div>
                        <div class="task-actions">
                            <button class="task-action-btn edit-btn" title="Редагувати">✏️</button>
                            <button class="task-action-btn delete-btn" title="Видалити">🗑️</button>
                        </div>
                    </li>
                `;
            });

            html += '</ul>';
            this.container.innerHTML = html;
        },

        attachEventListeners: function() {
            this.container.querySelectorAll('.task-checkbox').forEach(checkbox => {
                checkbox.addEventListener('change', (e) => {
                    const taskId = e.target.closest('.task-item').dataset.id;
                    this.toggleTask(taskId);
                });
            });

            this.container.querySelectorAll('.delete-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const taskId = e.target.closest('.task-item').dataset.id;
                    if (confirmAction('Ви впевнені, що хочете видалити це завдання?')) {
                        this.deleteTask(taskId);
                    }
                });
            });

            this.container.querySelectorAll('.edit-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const taskId = e.target.closest('.task-item').dataset.id;
                    this.editTask(taskId);
                });
            });
        },

        toggleTask: function(id) {
            const task = this.tasks.find(t => t.id == id);
            if (task) {
                task.completed = !task.completed;
                this.render();
                this.attachEventListeners();
            }
        },

        deleteTask: function(id) {
            this.tasks = this.tasks.filter(t => t.id != id);
            this.render();
            this.attachEventListeners();
        },

        editTask: function(id) {
            const task = this.tasks.find(t => t.id == id);
            if (task) {
                // Trigger edit event or modal
                const event = new CustomEvent('taskEdit', { detail: task });
                this.container.dispatchEvent(event);
            }
        },

        setFilter: function(type, value) {
            this.filters[type] = value;
            this.render();
            this.attachEventListeners();
        },

        getFilteredTasks: function() {
            return this.tasks.filter(task => {
                const statusMatch = this.filters.status === 'all' || 
                    (this.filters.status === 'completed' ? task.completed : !task.completed);
                const priorityMatch = this.filters.priority === 'all' || task.priority === this.filters.priority;
                return statusMatch && priorityMatch;
            });
        },

        escapeHtml: function(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
    };

    console.log('StudentHelper tasks.js initialized');
})();
