/* ===================================
   PERFUME STORE - ADMIN JAVASCRIPT
   =================================== */

document.addEventListener('DOMContentLoaded', function() {
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
        btn.addEventListener('click', async function() {
            const productId = this.dataset.productId;
            const isArabic = document.documentElement.lang === 'ar';
            
            if (confirm(isArabic ? 'هل أنت متأكد من حذف هذا المنتج؟' : 'Are you sure you want to delete this product?')) {
                try {
                    const response = await fetch(`/Admin/DeleteProduct/${productId}`, {
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                        }
                    });

                    if (response.ok) {
                        this.closest('tr').remove();
                        showNotification(isArabic ? 'تم حذف المنتج بنجاح' : 'Product deleted successfully', 'success');
                    }
                } catch (error) {
                    showNotification(isArabic ? 'حدث خطأ' : 'An error occurred', 'error');
                }
            }
        });
    });

    // Status Filter
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', function() {
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

    // Order Status Update
    document.querySelectorAll('.order-status-select').forEach(select => {
        select.addEventListener('change', async function() {
            const orderId = this.dataset.orderId;
            const status = this.value;

            try {
                const response = await fetch('/Admin/UpdateOrderStatus', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ orderId, status })
                });

                if (response.ok) {
                    const isArabic = document.documentElement.lang === 'ar';
                    showNotification(isArabic ? 'تم تحديث حالة الطلب' : 'Order status updated', 'success');
                    
                    // Update badge color
                    const badge = this.closest('tr').querySelector('.order-status');
                    badge.className = `order-status ${status.toLowerCase()}`;
                    badge.textContent = status;
                }
            } catch (error) {
                console.error('Error updating order status:', error);
            }
        });
    });

    // Notification
    function showNotification(message, type) {
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

    // Initialize Charts if Chart.js is available
    if (typeof Chart !== 'undefined') {
        // Charts are initialized in the view with inline scripts
    }

    // Data Tables initialization
    const dataTables = document.querySelectorAll('.data-table');
    dataTables.forEach(table => {
        // Add sorting functionality
        const headers = table.querySelectorAll('th');
        headers.forEach((header, index) => {
            header.style.cursor = 'pointer';
            header.addEventListener('click', () => sortTable(table, index));
        });
    });

    function sortTable(table, columnIndex) {
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        
        rows.sort((a, b) => {
            const aCell = a.querySelectorAll('td')[columnIndex];
            const bCell = b.querySelectorAll('td')[columnIndex];
            
            const aValue = aCell.textContent.trim();
            const bValue = bCell.textContent.trim();
            
            // Try to parse as number
            const aNum = parseFloat(aValue.replace(/[^0-9.-]/g, ''));
            const bNum = parseFloat(bValue.replace(/[^0-9.-]/g, ''));
            
            if (!isNaN(aNum) && !isNaN(bNum)) {
                return aNum - bNum;
            }
            
            return aValue.localeCompare(bValue);
        });

        // Toggle direction
        if (table.dataset.sortDirection === 'asc') {
            rows.reverse();
            table.dataset.sortDirection = 'desc';
        } else {
            table.dataset.sortDirection = 'asc';
        }

        rows.forEach(row => tbody.appendChild(row));
    }

    // Form validation
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const requiredFields = form.querySelectorAll('[required]');
            let isValid = true;

            requiredFields.forEach(field => {
                if (!field.value.trim()) {
                    isValid = false;
                    field.classList.add('error');
                } else {
                    field.classList.remove('error');
                }
            });

            if (!isValid) {
                e.preventDefault();
                const isArabic = document.documentElement.lang === 'ar';
                showNotification(isArabic ? 'يرجى ملء جميع الحقول المطلوبة' : 'Please fill in all required fields', 'error');
            }
        });
    });

    // Image preview for product form
    const imageInput = document.querySelector('input[type="file"][name="image"]');
    if (imageInput) {
        imageInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    let preview = document.querySelector('.image-preview');
                    if (!preview) {
                        preview = document.createElement('div');
                        preview.className = 'image-preview';
                        preview.style.cssText = 'margin-top: 1rem; border-radius: 8px; overflow: hidden;';
                        imageInput.parentElement.appendChild(preview);
                    }
                    preview.innerHTML = `<img src="${e.target.result}" alt="Preview" style="max-width: 200px; border-radius: 8px;">`;
                };
                reader.readAsDataURL(file);
            }
        });
    }
});
