/* ===================================
   PERFUME STORE - ADMIN JAVASCRIPT
   =================================== */

document.addEventListener('DOMContentLoaded', function () {
    // Sidebar Toggle
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', () => {
            sidebar.classList.toggle('active');
        });

        // Close sidebar when clicking outside on mobile
        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 1024) {
                if (!sidebar.contains(e.target) && !sidebarToggle.contains(e.target)) {
                    sidebar.classList.remove('active');
                }
            }
        });
    }

    // Delete Confirmation
    document.querySelectorAll('.delete').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            // If the button is inside a form, let the form submit logic handle it via onclick attribute in HTML
            // This is mainly for AJAX calls if implemented later
            if (!this.closest('form')) {
                const isArabic = document.documentElement.lang === 'ar';
                if (!confirm(isArabic ? 'هل أنت متأكد من الحذف؟' : 'Are you sure you want to delete?')) {
                    e.preventDefault();
                }
            }
        });
    });

    // Status Filter
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', function () {
            const status = this.value;
            const url = new URL(window.location);

            if (status) {
                url.searchParams.set('status', status);
            } else {
                url.searchParams.delete('status');
            }

            window.location.href = url.toString();
        });

        // Set current value
        const currentStatus = new URLSearchParams(window.location.search).get('status');
        if (currentStatus) {
            statusFilter.value = currentStatus;
        }
    }

    // Order Status Update (AJAX example)
    document.querySelectorAll('.order-status-select').forEach(select => {
        select.addEventListener('change', async function () {
            const orderId = this.dataset.orderId;
            const status = this.value;

            try {
                // If using AJAX:
                /*
                const response = await fetch('/Admin/UpdateOrderStatus', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ orderId, status })
                });
                */
                // Since we are submitting via form directly in HTML, this listener might just show visual feedback
            } catch (error) {
                console.error('Error updating order status:', error);
            }
        });
    });

    // Notification System
    window.showNotification = function (message, type) {
        const notification = document.createElement('div');
        notification.className = `admin-notification ${type}`;
        notification.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle'}"></i>
            <span>${message}</span>
        `;

        // Add styles if not exists
        if (!document.querySelector('#admin-notification-styles')) {
            const styles = document.createElement('style');
            styles.id = 'admin-notification-styles';
            styles.textContent = `
                .admin-notification {
                    position: fixed;
                    bottom: 20px;
                    right: 20px;
                    padding: 1rem 1.5rem;
                    background: var(--admin-card, #1a1a2e);
                    border: 1px solid var(--admin-border, rgba(212, 175, 55, 0.2));
                    border-radius: 10px;
                    display: flex;
                    align-items: center;
                    gap: 0.75rem;
                    z-index: 1000;
                    animation: slideUp 0.3s ease;
                    color: #fff;
                }
                .admin-notification.success { border-color: #27ae60; }
                .admin-notification.success i { color: #27ae60; }
                .admin-notification.error { border-color: #e74c3c; }
                .admin-notification.error i { color: #e74c3c; }
                @keyframes slideUp {
                    from { opacity: 0; transform: translateY(20px); }
                    to { opacity: 1; transform: translateY(0); }
                }
            `;
            document.head.appendChild(styles);
        }

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideUp 0.3s ease reverse';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Data Tables Sorting
    const dataTables = document.querySelectorAll('.data-table');
    dataTables.forEach(table => {
        const headers = table.querySelectorAll('th');
        headers.forEach((header, index) => {
            header.style.cursor = 'pointer';
            header.addEventListener('click', () => sortTable(table, index));
        });
    });

    function sortTable(table, columnIndex) {
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));

        const isNumeric = !isNaN(parseFloat(rows[0].querySelectorAll('td')[columnIndex].innerText.replace(/[^0-9.-]/g, '')));

        rows.sort((a, b) => {
            const aCell = a.querySelectorAll('td')[columnIndex].innerText.trim();
            const bCell = b.querySelectorAll('td')[columnIndex].innerText.trim();

            if (isNumeric) {
                const aNum = parseFloat(aCell.replace(/[^0-9.-]/g, ''));
                const bNum = parseFloat(bCell.replace(/[^0-9.-]/g, ''));
                return aNum - bNum;
            }
            return aCell.localeCompare(bCell);
        });

        if (table.dataset.sortDirection === 'asc') {
            rows.reverse();
            table.dataset.sortDirection = 'desc';
        } else {
            table.dataset.sortDirection = 'asc';
        }

        rows.forEach(row => tbody.appendChild(row));
    }

    // Image preview
    const imageInput = document.getElementById('imageInput');
    if (imageInput) {
        imageInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const preview = document.getElementById('imagePreview');
                    if (preview) {
                        preview.innerHTML = `<img src="${e.target.result}" alt="Preview" style="max-width: 200px; border-radius: 8px; margin-top: 10px;">`;
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }
});