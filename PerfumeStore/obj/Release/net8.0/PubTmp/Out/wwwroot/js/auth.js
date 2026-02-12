/* ===================================
   PERFUME STORE - AUTH JAVASCRIPT
   =================================== */

document.addEventListener('DOMContentLoaded', function() {
    // Toggle Password Visibility
    document.querySelectorAll('.toggle-password').forEach(btn => {
        btn.addEventListener('click', function() {
            const input = this.parentElement.querySelector('input');
            const icon = this.querySelector('i');
            
            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
            }
        });
    });

    // Form Validation
    const forms = document.querySelectorAll('.auth-form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            let isValid = true;
            const inputs = form.querySelectorAll('input[required]');
            
            inputs.forEach(input => {
                if (!input.value.trim()) {
                    isValid = false;
                    input.classList.add('error');
                } else {
                    input.classList.remove('error');
                }
            });

            // Email validation
            const emailInput = form.querySelector('input[type="email"]');
            if (emailInput && emailInput.value) {
                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                if (!emailRegex.test(emailInput.value)) {
                    isValid = false;
                    emailInput.classList.add('error');
                    const isArabic = document.documentElement.lang === 'ar';
                    showError(emailInput, isArabic ? 'البريد الإلكتروني غير صحيح' : 'Invalid email address');
                }
            }

            // Password confirmation
            const password = form.querySelector('input[name="Password"], input[name="NewPassword"]');
            const confirmPassword = form.querySelector('input[name="ConfirmPassword"]');
            if (password && confirmPassword && password.value !== confirmPassword.value) {
                isValid = false;
                confirmPassword.classList.add('error');
                const isArabic = document.documentElement.lang === 'ar';
                showError(confirmPassword, isArabic ? 'كلمات المرور غير متطابقة' : 'Passwords do not match');
            }

            if (!isValid) {
                e.preventDefault();
            }
        });
    });

    function showError(input, message) {
        let errorSpan = input.parentElement.querySelector('.error-message');
        if (!errorSpan) {
            errorSpan = document.createElement('span');
            errorSpan.className = 'error-message';
            input.parentElement.appendChild(errorSpan);
        }
        errorSpan.textContent = message;
    }

    // Remove error on input
    document.querySelectorAll('.auth-form input').forEach(input => {
        input.addEventListener('input', function() {
            this.classList.remove('error');
            const errorSpan = this.parentElement.querySelector('.error-message');
            if (errorSpan) {
                errorSpan.textContent = '';
            }
        });
    });

    // Social Login Handlers
    document.querySelectorAll('.social-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            // Add loading state
            this.classList.add('loading');
            this.disabled = true;
            
            // Simulate social login redirect
            setTimeout(() => {
                this.classList.remove('loading');
                this.disabled = false;
            }, 2000);
        });
    });

    // Add loading animation styles
    const style = document.createElement('style');
    style.textContent = `
        .social-btn.loading {
            opacity: 0.7;
            pointer-events: none;
        }
        .social-btn.loading::after {
            content: '';
            position: absolute;
            width: 20px;
            height: 20px;
            border: 2px solid transparent;
            border-top-color: currentColor;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
        }
        @keyframes spin {
            to { transform: rotate(360deg); }
        }
        input.error {
            border-color: #e74c3c !important;
            animation: shake 0.3s ease;
        }
        @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-5px); }
            75% { transform: translateX(5px); }
        }
    `;
    document.head.appendChild(style);
});
