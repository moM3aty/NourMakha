let currentLanguage = document.documentElement.lang || 'ar';

document.addEventListener('DOMContentLoaded', function () {
    // Initialize
    initializeApp();
});

/**
 * تهيئة التطبيق
 */
function initializeApp() {
    // Preloader
    initPreloader();

    // Header
    initHeader();

    // Mobile Menu
    initMobileMenu();

    // Search
    initSearch();

    // Cart
    initCart();

    // Toast System
    initToastSystem();

    // Animations
    initAnimations();
}

/**
 * Preloader
 */
function initPreloader() {
    const preloader = document.getElementById('preloader');
    if (preloader) {
        // إخفاء الـ preloader بعد تحميل الصفحة
        window.addEventListener('load', function () {
            setTimeout(() => {
                preloader.classList.add('hidden');
            }, 500);
        });
    }
}

/**
 * Header Scroll Effect
 */
function initHeader() {
    const header = document.querySelector('.header');
    if (header) {
        let lastScrollY = 0;

        window.addEventListener('scroll', () => {
            const currentScrollY = window.scrollY;

            if (currentScrollY > 50) {
                header.classList.add('scrolled');
            } else {
                header.classList.remove('scrolled');
            }

            lastScrollY = currentScrollY;
        }, { passive: true });
    }
}

/**
 * Mobile Menu
 */
function initMobileMenu() {
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const navMenu = document.getElementById('navMenu');

    if (mobileMenuBtn && navMenu) {
        // Toggle Menu
        mobileMenuBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            navMenu.classList.toggle('active');

            // تغيير الأيقونة
            const icon = mobileMenuBtn.querySelector('i');
            if (icon) {
                icon.classList.toggle('fa-bars');
                icon.classList.toggle('fa-times');
            }
        });

        // إغلاق القائمة عند النقر خارجه
        document.addEventListener('click', (e) => {
            if (navMenu.classList.contains('active')) {
                if (!navMenu.contains(e.target) && !mobileMenuBtn.contains(e.target)) {
                    navMenu.classList.remove('active');
                    const icon = mobileMenuBtn.querySelector('i');
                    if (icon) {
                        icon.classList.add('fa-bars');
                        icon.classList.remove('fa-times');
                    }
                }
            }
        });

        // إغلاق القائمة عند النقر على رابط
        navMenu.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', () => {
                navMenu.classList.remove('active');
                const icon = mobileMenuBtn.querySelector('i');
                if (icon) {
                    icon.classList.add('fa-bars');
                    icon.classList.remove('fa-times');
                }
            });
        });
    }
}

/**
 * Search Overlay
 */
function initSearch() {
    const searchBtn = document.getElementById('searchBtn');
    const searchOverlay = document.getElementById('searchOverlay');
    const searchClose = document.getElementById('searchClose');

    if (searchBtn && searchOverlay) {
        // فتح البحث
        searchBtn.addEventListener('click', () => {
            searchOverlay.classList.add('active');
            const searchInput = searchOverlay.querySelector('.search-input');
            if (searchInput) {
                setTimeout(() => searchInput.focus(), 100);
            }
        });

        // إغلاق البحث
        if (searchClose) {
            searchClose.addEventListener('click', () => {
                searchOverlay.classList.remove('active');
            });
        }

        // إغلاق عند النقر على الخلفية
        searchOverlay.addEventListener('click', (e) => {
            if (e.target === searchOverlay) {
                searchOverlay.classList.remove('active');
            }
        });

        // إغلاق بزر Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && searchOverlay.classList.contains('active')) {
                searchOverlay.classList.remove('active');
            }
        });
    }
}

/**
 * Cart Functions
 */
function initCart() {
    // تحديث عدد المنتجات
    updateCartCount();

    // إضافة أزرار "أضف للسلة"
    document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.preventDefault();

            const productId = this.dataset.productId;
            if (!productId) return;

            // إضافة تأثير التحميل
            this.classList.add('loading');
            this.disabled = true;

            try {
                const response = await fetch('/Cart/Add', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify({
                        productId: parseInt(productId),
                        quantity: 1
                    })
                });

                const result = await response.json();

                if (result.success) {
                    showToast('success', getLocalizedString('addedToCart'));
                    updateCartCount();

                    // تأثير النجاح
                    this.classList.add('added');
                    setTimeout(() => this.classList.remove('added'), 300);
                } else {
                    showToast('error', result.message || getLocalizedString('errorOccurred'));
                }
            } catch (error) {
                console.error('Error adding to cart:', error);
                showToast('error', getLocalizedString('errorOccurred'));
            } finally {
                this.classList.remove('loading');
                this.disabled = false;
            }
        });
    });
}

/**
 * تحديث عدد المنتجات في السلة
 */
async function updateCartCount() {
    try {
        const response = await fetch('/Cart/GetCartCount');
        const result = await response.json();

        const cartCountElements = document.querySelectorAll('.cart-count');
        cartCountElements.forEach(el => {
            el.textContent = result.count || 0;

            // تأثير التحديث
            el.style.transform = 'scale(1.2)';
            setTimeout(() => {
                el.style.transform = 'scale(1)';
            }, 200);
        });
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

/**
 * Toast System
 */
function initToastSystem() {
    // إنشاء حاوية الـ Toast إذا لم تكن موجودة
    if (!document.getElementById('toastContainer')) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
}

/**
 * عرض رسالة Toast
 * @param {string} type - نوع الرسالة: 'success' أو 'error'
 * @param {string} message - نص الرسالة
 * @param {number} duration - مدة العرض بالميلي ثانية
 */
function showToast(type, message, duration = 3000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

    toast.innerHTML = `
        <i class="fas ${icon}"></i>
        <span>${message}</span>
    `;

    container.appendChild(toast);

    // إزالة الـ Toast بعد المدة المحددة
    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease forwards';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 300);
    }, duration);
}

/**
 * Animations
 */
function initAnimations() {
    // Intersection Observer للعناصر المتحركة
    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-visible');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.animate-on-scroll').forEach(el => {
        observer.observe(el);
    });
}

/**
 * الحصول على نص مترجم
 * @param {string} key - مفتاح النص
 * @returns {string} النص المترجم
 */
function getLocalizedString(key) {
    const isArabic = currentLanguage.startsWith('ar');

    const strings = {
        addedToCart: {
            en: 'Added to cart successfully',
            ar: 'تمت الإضافة للسلة بنجاح'
        },
        errorOccurred: {
            en: 'An error occurred',
            ar: 'حدث خطأ، يرجى المحاولة مرة أخرى'
        },
        addedToWishlist: {
            en: 'Added to wishlist',
            ar: 'تمت الإضافة للمفضلة'
        },
        removedFromWishlist: {
            en: 'Removed from wishlist',
            ar: 'تمت الإزالة من المفضلة'
        },
        confirmDelete: {
            en: 'Are you sure you want to delete this item?',
            ar: 'هل أنت متأكد من حذف هذا العنصر؟'
        }
    };

    return strings[key]?.[isArabic ? 'ar' : 'en'] || key;
}

/**
 * الحصول على Anti-Forgery Token
 * @returns {string} التوكن
 */
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

/**
 * تبديل المفضلة
 * @param {number} productId - معرف المنتج
 */
async function toggleWishlist(productId) {
    try {
        const response = await fetch('/Wishlist/Toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: `productId=${productId}`
        });

        const result = await response.json();

        if (result.success) {
            showToast('success', result.message);

            // تحديث أيقونة القلب
            const btn = document.querySelector(`.wishlist-btn[data-product-id="${productId}"]`);
            if (btn) {
                const icon = btn.querySelector('i');
                if (icon) {
                    if (result.inWishlist) {
                        icon.classList.remove('far');
                        icon.classList.add('fas');
                    } else {
                        icon.classList.remove('fas');
                        icon.classList.add('far');
                    }
                }
            }
        }
    } catch (error) {
        console.error('Error toggling wishlist:', error);
        showToast('error', getLocalizedString('errorOccurred'));
    }
}

/**
 * إضافة منتج للسلة مع كمية محددة
 * @param {number} productId - معرف المنتج
 * @param {number} quantity - الكمية
 */
async function addToCartWithQty(productId, quantity = 1) {
    try {
        const response = await fetch('/Cart/Add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                productId: productId,
                quantity: quantity
            })
        });

        const result = await response.json();

        if (result.success) {
            showToast('success', getLocalizedString('addedToCart'));
            updateCartCount();
        } else {
            showToast('error', result.message || getLocalizedString('errorOccurred'));
        }
    } catch (error) {
        console.error('Error adding to cart:', error);
        showToast('error', getLocalizedString('errorOccurred'));
    }
}

/**
 * تنسيق الأرقام
 * @param {number} number - الرقم
 * @param {number} decimals - عدد الأرقام العشرية
 * @returns {string} الرقم منسق
 */
function formatNumber(number, decimals = 2) {
    return number.toLocaleString(currentLanguage === 'ar' ? 'ar-OM' : 'en-US', {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    });
}

/**
 * تنسيق العملة
 * @param {number} amount - المبلغ
 * @param {string} currency - رمز العملة
 * @returns {string} المبلغ منسق
 */
function formatCurrency(amount, currency = 'OMR') {
    return `${formatNumber(amount)} ${currency}`;
}

// إضافة تأثيرات CSS للتحريك
const style = document.createElement('style');
style.textContent = `
    @keyframes slideOut {
        from { 
            opacity: 1; 
            transform: translateX(0); 
        }
        to { 
            opacity: 0; 
            transform: translateX(100%); 
        }
    }
    
    .add-to-cart-btn.loading {
        pointer-events: none;
        opacity: 0.7;
    }
    
    .add-to-cart-btn.loading::after {
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
    
    .add-to-cart-btn.added {
        background: #27ae60 !important;
        transform: scale(1.05);
    }
    
    .animate-visible {
        animation: fadeInUp 0.6s ease forwards;
    }
    
    @keyframes fadeInUp {
        from {
            opacity: 0;
            transform: translateY(30px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
`;
document.head.appendChild(style);

// تصدير الدوال للاستخدام الخارجي
window.showToast = showToast;
window.updateCartCount = updateCartCount;
window.toggleWishlist = toggleWishlist;
window.addToCartWithQty = addToCartWithQty;
window.formatNumber = formatNumber;
window.formatCurrency = formatCurrency;
window.getLocalizedString = getLocalizedString;
