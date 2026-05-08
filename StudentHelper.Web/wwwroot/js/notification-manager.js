/**
 * ������� ����������� � ��������� ��� � ������������� SignalR
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
        this.broadcastChannel = null;
        this.initBroadcastChannel();
    }

    /**
     * Ініціалізація Broadcast Channel для синхронізації між вкладками
     */
    initBroadcastChannel() {
        try {
            this.broadcastChannel = new BroadcastChannel('notifications');
            this.broadcastChannel.onmessage = (event) => {
                const { type, data } = event.data;
                
                console.log('📨 Отримано повідомлення через Broadcast Channel:', { type, data });
                
                if (type === 'notification') {
                    // Отримано сповіщення з іншої вкладки
                    console.log('📢 Обробка синхронізованого сповіщення');
                    this.handleNewNotification(data);
                    console.log('✅ Сповіщення синхронізовано з іншої вкладки');
                } else if (type === 'unreadCount') {
                    // Синхронізація лічильника непрочитаних
                    console.log('🔢 Оновлення лічильника: ', data);
                    this.unreadCount = data;
                    this.updateUnreadBadge();
                }
            };
            console.log('✅ Broadcast Channel ініціалізовано для синхronізації вкладок');
        } catch (error) {
            console.warn('⚠️ Broadcast Channel недоступний (приватне вікно?):', error);
        }
    }

    /**
     * Трансмітування сповіщення на інші вкладки
     */
    broadcastNotification(notification) {
        if (this.broadcastChannel) {
            try {
                this.broadcastChannel.postMessage({
                    type: 'notification',
                    data: notification
                });
            } catch (error) {
                console.warn('⚠️ Помилка при трансмітуванні сповіщення:', error);
            }
        }
    }

    /**
     * Трансмітування оновлення лічильника на інші вкладки
     */
    broadcastUnreadCount(count) {
        if (this.broadcastChannel) {
            try {
                this.broadcastChannel.postMessage({
                    type: 'unreadCount',
                    data: count
                });
            } catch (error) {
                console.warn('⚠️ Помилка при трансмітуванні лічильника:', error);
            }
        }
    }

    /**
     * ��������� ���������� �� SignalR Hub
     */
    async initializeConnection() {
        try {
            // ��������� SignalR ��������
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

            // ϳ�������� ������� ����
            this.setupEventListeners();

            // �������� ����������
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log('? ϳ�������� �� SignalR Hub');
            await this.loadUnreadCount();

            return true;
        } catch (error) {
            console.error('? ������� ��� ���������� �� SignalR Hub:', error);
            this.handleConnectionError();
            return false;
        }
    }

    /**
     * ��������� �������� ���� �� �������
     */
    setupEventListeners() {
        if (!this.connection) return;

        // ������� ����������� � ��������� ���
        this.connection.on('ReceiveNotification', (notification) => {
            console.log('?? ���� �����������:', notification);
            this.handleNewNotification(notification);
        });

        // ������� ���������������
        this.connection.onreconnecting(() => {
            console.log('?? ������ ��������������� �� SignalR...');
            this.isConnected = false;
        });

        this.connection.onreconnected(() => {
            console.log('? �������������� �� SignalR');
            this.isConnected = true;
        });

        this.connection.onclose(() => {
            console.log('? ³�������� �� SignalR');
            this.isConnected = false;
        });
    }

    /**
     * �������� ���� �����������
     */
    handleNewNotification(notification) {
        // ������ ����������� �� ������
        this.notifications.unshift(notification);

        // �������� ���������� �����������
        this.showToastNotification(notification);

        // ��������� UI
        this.updateNotificationPanel();
        this.incrementUnreadCount();
    }

    /**
     * ������ ���������� ����������� (toast)
     */
    showToastNotification(notification) {
        // ����������, �� � ��� ����� ���������
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

        // ����������� ��������� ����� 6 ������
        setTimeout(() => {
            toastElement.remove();
        }, 6000);

        // �������� ������ (�����������)
        this.playNotificationSound();
    }

    /**
     * ������� ��������� ��� ����������� �����������
     */
    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'notification-toast-container';
        container.className = 'notification-toast-container';
        document.body.appendChild(container);
        return container;
    }

    /**
     * ������� ������ ����������� � ����
     */
    async updateNotificationPanel() {
        try {
            const response = await fetch('/api/notifications/unread');
            const notifications = await response.json();

            // ��������� ������� ��� ����������� �����������
            const notificationPanel = document.getElementById('notification-panel');
            if (notificationPanel) {
                notificationPanel.innerHTML = '';

                if (notifications.length === 0) {
                    notificationPanel.innerHTML = '<p class="text-muted">���� �����������</p>';
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
            console.error('������� ��� ��������� ����� �����������:', error);
        }
    }

    /**
     * ��������� ������� ������������ �����������
     */
    async loadUnreadCount() {
        try {
            const response = await fetch('/api/notifications/unread-count');
            const data = await response.json();
            this.unreadCount = data.unreadCount;
            this.updateUnreadBadge();
        } catch (error) {
            console.error('������� ��� ������������ ������� ������������:', error);
        }
    }

    /**
     * ������� ����� � ������� ������������ �����������
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
     * Збільшення лічильника непрочитаних сповіщень
     */
    incrementUnreadCount() {
        this.unreadCount++;
        this.updateUnreadBadge();
        this.broadcastUnreadCount(this.unreadCount);
    }

    /**
     * Позначити сповіщення як прочитане
     */
    async markAsRead(notificationId) {
        try {
            await fetch(`/api/notifications/${notificationId}/mark-read`, {
                method: 'POST'
            });
            await this.loadUnreadCount();
            await this.updateNotificationPanel();
            this.broadcastUnreadCount(this.unreadCount);
        } catch (error) {
            console.error('Помилка при позначенні сповіщення як прочитаного:', error);
        }
    }

    /**
     * Позначити всі сповіщення як прочитані
     */
    async markAllAsRead() {
        try {
            await fetch('/api/notifications/mark-all-read', {
                method: 'POST'
            });
            this.unreadCount = 0;
            this.updateUnreadBadge();
            await this.updateNotificationPanel();
            this.broadcastUnreadCount(this.unreadCount);
        } catch (error) {
            console.error('Помилка при позначенні всіх сповіщень як прочитаних:', error);
        }
    }

    /**
     * Видалити сповіщення
     */
    async deleteNotification(notificationId) {
        try {
            await fetch(`/api/notifications/${notificationId}`, {
                method: 'DELETE'
            });
            await this.loadUnreadCount();
            await this.updateNotificationPanel();
            this.broadcastUnreadCount(this.unreadCount);
        } catch (error) {
            console.error('Помилка при видаленні сповіщення:', error);
        }
    }

    /**
     * ������� ��� �����������
     */
    formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return '�����';
        if (diffMins < 60) return `${diffMins} �� ����`;
        if (diffHours < 24) return `${diffHours} ��� ����`;
        if (diffDays < 7) return `${diffDays} �� ����`;

        return date.toLocaleDateString('uk-UA');
    }

    /**
     * ³������� �������� ������ �����������
     */
    playNotificationSound() {
        // ���� � �������� ����, �������������:
        // const audio = new Audio('/sounds/notification.mp3');
        // audio.play().catch(e => console.log('Cannot play sound:', e));
    }

    /**
     * �������� ������� ����������
     */
    handleConnectionError() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`?? ������ ��������������� ${this.reconnectAttempts}/${this.maxReconnectAttempts} �� ${this.reconnectDelay}ms...`);
            setTimeout(() => this.initializeConnection(), this.reconnectDelay);
        } else {
            console.error('? �� ������� ����������� �� ������� ����������� ���� ' +
                `${this.maxReconnectAttempts} �����`);
        }
    }

    /**
     * ������� ����������
     */
    disconnect() {
        if (this.connection) {
            this.connection.stop()
                .then(() => console.log('? ³�������� �� SignalR Hub'))
                .catch(error => console.error('? ������� ��� ���������� ����������:', error));
        }
    }

    /**
     * ³�������� ������� ����������� ����� API � ������ �� ��������
     */
    async sendTestNotification() {
        try {
            const response = await fetch('/api/notifications/send-test', {
                method: 'POST',
                credentials: 'same-origin'
            });

            if (!response.ok) {
                console.error('������� ��� �������� ������� �����������', response.status);
                return;
            }

            const notification = await response.json();

            // ������ � ��������� ������ � �������� toast
            this.notifications.unshift(notification);
            this.showToastNotification(notification);
            this.incrementUnreadCount();
            await this.updateNotificationPanel();
        }
        catch (error) {
            console.error('������� ��� �������� ������� �����������:', error);
        }
    }
}

// ��������� ����� ��� ��������� �������������
let notificationManager = null;

/**
 * ��������� ������� ����������� ��� ������������ �������
 */
document.addEventListener('DOMContentLoaded', async () => {
    notificationManager = new NotificationManager();

    // ����������, �� ���������� ����������������
    try {
        const response = await fetch('/api/notifications/unread-count');
        if (response.ok) {
            // ���������� ����������������, ����������� �� SignalR
            await notificationManager.initializeConnection();
        }
    } catch (error) {
        console.log('���������� �� ����������������');
    }

    // ������ ��� Bootstrap dropdown ����
    const notificationBell = document.getElementById('notificationBell');
    if (notificationBell) {
        notificationBell.addEventListener('click', function () {
            // ҳ ̲ ̲ ̲ ̲ ̲ ̲ Bootstrap Dropdown ̲ ̲ ̲ ̲ ̲ 
            // Bootstrap ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ data-bs-toggle="dropdown"
            // ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ ̲ 
            setTimeout(() => {
                const panel = document.getElementById('notification-panel');
                if (panel && panel.classList.contains('show')) {
                    notificationManager.updateNotificationPanel();
                }
            }, 10);
        });
    }
});

/**
 * ������������� ����������� ��� ����� � �������
 */
window.addEventListener('beforeunload', () => {
    if (notificationManager) {
        notificationManager.disconnect();
    }
});
