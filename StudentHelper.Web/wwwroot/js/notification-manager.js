/**
 * Система нотифікацій у реальному часі з використанням SignalR
 */

class NotificationManager {
    constructor() {
        this.connection = null;
        this.notifications = [];
        this.unreadCount = 0;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
    }

    /**
     * Ініціалізує підключення до SignalR Hub
     */
    async initializeConnection() {
        try {
            // Імпортуємо SignalR бібліотеку
            const { HubConnectionBuilder, LogLevel } = window;

            if (!HubConnectionBuilder) {
                console.error('SignalR library not loaded. Make sure to include @microsoft/signalr script.');
                return false;
            }

            this.connection = new HubConnectionBuilder()
                .withUrl('/hubs/notification')
                .withAutomaticReconnect([0, 0, 1000, 3000, 5000, 10000])
                .configureLogging(LogLevel.Information)
                .build();

            // Підключаємо слухачі подій
            this.setupEventListeners();

            // Стартуємо підключення
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log('? Підключено до SignalR Hub');
            await this.loadUnreadCount();

            return true;
        } catch (error) {
            console.error('? Помилка при підключенні до SignalR Hub:', error);
            this.handleConnectionError();
            return false;
        }
    }

    /**
     * Налаштовує слухачів подій від сервера
     */
    setupEventListeners() {
        if (!this.connection) return;

        // Слухаємо нотифікації у реальному часі
        this.connection.on('ReceiveNotification', (notification) => {
            console.log('?? Нова нотифікація:', notification);
            this.handleNewNotification(notification);
        });

        // Обробка переподключення
        this.connection.onreconnecting(() => {
            console.log('?? Спроба переподключення до SignalR...');
            this.isConnected = false;
        });

        this.connection.onreconnected(() => {
            console.log('? Переподключено до SignalR');
            this.isConnected = true;
        });

        this.connection.onclose(() => {
            console.log('? Відключено від SignalR');
            this.isConnected = false;
        });
    }

    /**
     * Обробляє нову нотифікацію
     */
    handleNewNotification(notification) {
        // Додаємо нотифікацію до масиву
        this.notifications.unshift(notification);

        // Показуємо вспливаючу нотифікацію
        this.showToastNotification(notification);

        // Оновлюємо UI
        this.updateNotificationPanel();
        this.incrementUnreadCount();
    }

    /**
     * Показує вспливаючу нотифікацію (toast)
     */
    showToastNotification(notification) {
        // Перевіряємо, чи є для цього контейнер
        const toastContainer = document.getElementById('notification-toast-container') ||
            this.createToastContainer();

        const toastElement = document.createElement('div');
        toastElement.className = `notification-toast notification-${notification.type}`;
        toastElement.innerHTML = `
            <div class="notification-toast-content">
                <div class="notification-toast-header">
                    <span class="notification-toast-icon">
                        <i class="bi ${notification.icon || 'bi-info-circle'}"></i>
                    </span>
                    <strong>${notification.title}</strong>
                    <button class="notification-toast-close" onclick="this.parentElement.parentElement.parentElement.remove()">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="notification-toast-body">
                    ${notification.message}
                </div>
                <div class="notification-toast-footer">
                    <small>${new Date().toLocaleTimeString('uk-UA')}</small>
                </div>
            </div>
        `;

        toastContainer.appendChild(toastElement);

        // Автоматично видаляємо через 6 секунд
        setTimeout(() => {
            toastElement.remove();
        }, 6000);

        // Звуковий сигнал (опціонально)
        this.playNotificationSound();
    }

    /**
     * Створює контейнер для вспливаючих нотифікацій
     */
    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'notification-toast-container';
        container.className = 'notification-toast-container';
        document.body.appendChild(container);
        return container;
    }

    /**
     * Оновлює панель нотифікацій у меню
     */
    async updateNotificationPanel() {
        try {
            const response = await fetch('/api/notifications/unread');
            const notifications = await response.json();

            // Знаходимо елемент для відображення нотифікацій
            const notificationPanel = document.getElementById('notification-panel');
            if (notificationPanel) {
                notificationPanel.innerHTML = '';

                if (notifications.length === 0) {
                    notificationPanel.innerHTML = '<p class="text-muted">Немає нотифікацій</p>';
                } else {
                    notifications.forEach(notif => {
                        const item = document.createElement('div');
                        item.className = 'notification-item';
                        item.innerHTML = `
                            <div class="notification-item-icon">
                                <i class="bi ${notif.icon || 'bi-bell'}"></i>
                            </div>
                            <div class="notification-item-content">
                                <div class="notification-item-title">${notif.title}</div>
                                <div class="notification-item-message">${notif.message.substring(0, 100)}...</div>
                                <small class="notification-item-time">${this.formatTime(notif.createdAt)}</small>
                            </div>
                            <button class="notification-item-close" data-id="${notif.id}" onclick="notificationManager.deleteNotification(${notif.id})">
                                <i class="bi bi-x"></i>
                            </button>
                        `;
                        notificationPanel.appendChild(item);
                    });
                }
            }
        } catch (error) {
            console.error('Помилка при оновленні панелі нотифікацій:', error);
        }
    }

    /**
     * Завантажує кількість непрочитаних нотифікацій
     */
    async loadUnreadCount() {
        try {
            const response = await fetch('/api/notifications/unread-count');
            const data = await response.json();
            this.unreadCount = data.unreadCount;
            this.updateUnreadBadge();
        } catch (error) {
            console.error('Помилка при завантаженні кількості непрочитаних:', error);
        }
    }

    /**
     * Оновлює бейдж з кількістю непрочитаних нотифікацій
     */
    updateUnreadBadge() {
        const badge = document.getElementById('notification-unread-badge');
        if (badge) {
            if (this.unreadCount > 0) {
                badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    /**
     * Збільшує лічильник непрочитаних нотифікацій
     */
    incrementUnreadCount() {
        this.unreadCount++;
        this.updateUnreadBadge();
    }

    /**
     * Позначає нотифікацію як прочитану
     */
    async markAsRead(notificationId) {
        try {
            await fetch(`/api/notifications/${notificationId}/mark-read`, {
                method: 'POST'
            });
            await this.loadUnreadCount();
            await this.updateNotificationPanel();
        } catch (error) {
            console.error('Помилка при позначенні нотифікації як прочитаної:', error);
        }
    }

    /**
     * Позначає всі нотифікації як прочитані
     */
    async markAllAsRead() {
        try {
            await fetch('/api/notifications/mark-all-read', {
                method: 'POST'
            });
            this.unreadCount = 0;
            this.updateUnreadBadge();
            await this.updateNotificationPanel();
        } catch (error) {
            console.error('Помилка при позначенні всіх нотифікацій як прочитаних:', error);
        }
    }

    /**
     * Видаляє нотифікацію
     */
    async deleteNotification(notificationId) {
        try {
            await fetch(`/api/notifications/${notificationId}`, {
                method: 'DELETE'
            });
            await this.loadUnreadCount();
            await this.updateNotificationPanel();
        } catch (error) {
            console.error('Помилка при видаленні нотифікації:', error);
        }
    }

    /**
     * Форматує час нотифікації
     */
    formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Щойно';
        if (diffMins < 60) return `${diffMins} хв тому`;
        if (diffHours < 24) return `${diffHours} год тому`;
        if (diffDays < 7) return `${diffDays} дн тому`;

        return date.toLocaleDateString('uk-UA');
    }

    /**
     * Відтворює звуковий сигнал нотифікації
     */
    playNotificationSound() {
        // Якщо є звуковий файл, розкоментуйте:
        // const audio = new Audio('/sounds/notification.mp3');
        // audio.play().catch(e => console.log('Cannot play sound:', e));
    }

    /**
     * Обробляє помилку підключення
     */
    handleConnectionError() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`?? Спроба переподключення ${this.reconnectAttempts}/${this.maxReconnectAttempts} за ${this.reconnectDelay}ms...`);
            setTimeout(() => this.initializeConnection(), this.reconnectDelay);
        } else {
            console.error('? Не вдалося підключитися до сервера нотифікацій після ' +
                `${this.maxReconnectAttempts} спроб`);
        }
    }

    /**
     * Розриває підключення
     */
    disconnect() {
        if (this.connection) {
            this.connection.stop()
                .then(() => console.log('? Відключено від SignalR Hub'))
                .catch(error => console.error('? Помилка при розриванні підключення:', error));
        }
    }

    /**
     * Відправляє тестову нотифікацію через API і показує її локально
     */
    async sendTestNotification() {
        try {
            const response = await fetch('/api/notifications/send-test', {
                method: 'POST',
                credentials: 'same-origin'
            });

            if (!response.ok) {
                console.error('Помилка при відправці тестової нотифікації', response.status);
                return;
            }

            const notification = await response.json();

            // Додати в локальний список і показати toast
            this.notifications.unshift(notification);
            this.showToastNotification(notification);
            this.incrementUnreadCount();
            await this.updateNotificationPanel();
        }
        catch (error) {
            console.error('Помилка при відправці тестової нотифікації:', error);
        }
    }
}

// Глобальна змінна для управління нотифікаціями
let notificationManager = null;

/**
 * Ініціалізує систему нотифікацій при завантаженні сторінки
 */
document.addEventListener('DOMContentLoaded', async () => {
    notificationManager = new NotificationManager();

    // Перевіряємо, чи користувач аутентифікований
    try {
        const response = await fetch('/api/notifications/unread-count');
        if (response.ok) {
            // Користувач аутентифікований, підключаємось до SignalR
            await notificationManager.initializeConnection();
        }
    } catch (error) {
        console.log('Користувач не аутентифікований');
    }
});

/**
 * Розпорядження підключенням при виході зі сторінки
 */
window.addEventListener('beforeunload', () => {
    if (notificationManager) {
        notificationManager.disconnect();
    }
});
