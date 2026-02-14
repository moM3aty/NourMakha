/* ===================================
   PERFUME STORE - ADMIN JAVASCRIPT
   =================================== */

document.addEventListener('DOMContentLoaded', function () {
    initSidebar();
    initDeleteConfirmation();
    initCustomSelects();
    initImagePreview();
});

/**
 * 1. Sidebar Toggle Logic (Desktop & Mobile)
 */
function initSidebar() {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const mainContent = document.querySelector('.admin-main');

    if (!sidebar || !sidebarToggle) return;

    // Toggle Click Event
    sidebarToggle.addEventListener('click', (e) => {
        e.stopPropagation();

        if (window.innerWidth > 1024) {
            // --- Desktop Logic (Collapse/Expand) ---
            sidebar.classList.toggle('collapsed');

            if (sidebar.classList.contains('collapsed')) {
                // Sidebar is hidden -> Expand content
                mainContent.style.marginInlineStart = '0';
                mainContent.style.width = '100%';
            } else {
                // Sidebar is visible -> Shrink content
                mainContent.style.marginInlineStart = '280px';
                mainContent.style.width = 'calc(100% - 280px)';
            }
        } else {
            // --- Mobile Logic (Off-canvas Overlay) ---
            sidebar.classList.toggle('active');
        }
    });

    // Close sidebar when clicking outside (Mobile only)
    document.addEventListener('click', (e) => {
        if (window.innerWidth <= 1024 && sidebar.classList.contains('active')) {
            if (!sidebar.contains(e.target) && !sidebarToggle.contains(e.target)) {
                sidebar.classList.remove('active');
            }
        }
    });

    // Handle Window Resize
    window.addEventListener('resize', () => {
        if (window.innerWidth > 1024) {
            // Reset mobile state
            sidebar.classList.remove('active');

            // Apply desktop state based on 'collapsed' class
            if (sidebar.classList.contains('collapsed')) {
                mainContent.style.marginInlineStart = '0';
                mainContent.style.width = '100%';
            } else {
                mainContent.style.marginInlineStart = '280px';
                mainContent.style.width = 'calc(100% - 280px)';
            }
        } else {
            // Reset desktop styles for mobile
            mainContent.style.marginInlineStart = '0';
            mainContent.style.width = '100%';
        }
    });
}

/**
 * 2. Delete Confirmation
 */
function initDeleteConfirmation() {
    document.querySelectorAll('.delete').forEach(btn => {
        // Only attach if it doesn't already have an inline onclick
        if (!btn.getAttribute('onclick')) {
            btn.addEventListener('click', function (e) {
                if (!this.closest('form')) {
                    const isArabic = document.documentElement.lang === 'ar';
                    if (!confirm(isArabic ? 'هل أنت متأكد من الحذف؟' : 'Are you sure you want to delete?')) {
                        e.preventDefault();
                    }
                }
            });
        }
    });
}

/**
 * 3. Custom "Other" Option in Select Lists
 */
function initCustomSelects() {
    const selects = document.querySelectorAll('select.allow-other');

    selects.forEach(select => {
        const input = select.nextElementSibling; // The hidden input field
        if (!input || !input.classList.contains('custom-other-input')) return;

        // Store original name
        const originalName = select.getAttribute('name');
        select.dataset.originalName = originalName;

        // Check initial value (Edit Mode)
        const currentValue = select.dataset.currentValue;
        if (currentValue) {
            let exists = false;
            for (let i = 0; i < select.options.length; i++) {
                if (select.options[i].value === currentValue) {
                    exists = true;
                    select.value = currentValue;
                    break;
                }
            }

            if (!exists && currentValue !== "") {
                select.value = "Other";
                input.value = currentValue;
                input.style.display = 'block';
                input.setAttribute('name', originalName);
                select.removeAttribute('name');
            }
        }

        // Handle Change
        select.addEventListener('change', function () {
            if (this.value === 'Other') {
                input.style.display = 'block';
                input.value = '';
                input.focus();
                input.setAttribute('name', this.dataset.originalName);
                this.removeAttribute('name');
            } else {
                input.style.display = 'none';
                this.setAttribute('name', this.dataset.originalName);
                input.removeAttribute('name');
            }
        });
    });
}

/**
 * 4. Image Preview
 */
function initImagePreview() {
    const imageInput = document.getElementById('imageInput');
    if (imageInput) {
        imageInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const preview = document.getElementById('imagePreview');
                    if (preview) {
                        preview.innerHTML = `<img src="${e.target.result}" alt="Preview" style="max-width: 100%; border-radius: 12px; border: 1px solid var(--gold-primary, #002855); margin-top: 10px;">`;
                    }

                    // Update upload UI feedback
                    const label = document.querySelector('.upload-label');
                    if (label) label.style.borderColor = '#2ecc71';

                    const icon = document.querySelector('.upload-icon');
                    if (icon) {
                        icon.className = 'fas fa-check-circle upload-icon';
                        icon.style.color = '#2ecc71';
                    }

                    const text = document.querySelector('.upload-text');
                    if (text) text.textContent = document.documentElement.lang === 'ar' ? 'تم اختيار الصورة' : 'Image Selected';
                };
                reader.readAsDataURL(file);
            }
        });
    }
}