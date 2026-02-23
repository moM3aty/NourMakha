/* ===================================
   PERFUME STORE - MAIN JAVASCRIPT (PREMIUM)
   =================================== */

let currentLanguage = document.documentElement.lang || 'ar';

document.addEventListener('DOMContentLoaded', function () {
    // تهيئة جميع المكونات عند تحميل الصفحة
    initializeApp();
});

/**
 * وظيفة التهيئة الرئيسية
 */
function initializeApp() {
    initPreloader();
    initHeader();
    initMobileMenu();
    initSearch();
    initCart();
    initToastSystem();
    initAnimations();
    initCurrencySwitcher();
}

/* ===================================
   نظام تبديل العملات (الريال العماني هو الأساس)
   =================================== */
const currencyRates = {
    "OMR": {
        rate: 1,
        symbol: '<svg width="25" height="25" viewBox="0 0 500 500" fill="currentColor" style="vertical-align: middle; margin-inline-start: 5px;"><path d="M241.67,213.77c-.63-49.2,11.44-95.41,35.76-137.75C313.47,13.28,353.02-6.48,421.55,28.87c10.67,5.5,53.6,35.43,57.81,44.54,5.03,10.87-27.48,103.87-29.11,122.3-34.69-37.51-99.37-98.66-154.85-69.62-45.05,23.58-12.02,62.54,11.46,87.68h409.36l-26.41,47.64h-332.5c-.31,1.8.87,3.3,2.53,4.6,12.44,9.72,80.97,39.54,94.75,39.54h210.71l-26.89,48.94H13.37l26.91-48.94h253.38l-37.11-44.13H64.75l26.41-47.64h150.51Z"/></svg>',
        flag: "🇴🇲", name: "ريال عماني"
    },
    "SAR": { rate: 9.74, symbol: "SAR", flag: "🇸🇦", name: "ريال سعودي" },
    "AED": { rate: 9.54, symbol: "AED", flag: "🇦🇪", name: "درهم إماراتي" },
    "KWD": { rate: 0.80, symbol: "KWD", flag: "🇰🇼", name: "دينار كويتي" },
    "QAR": { rate: 9.46, symbol: "QAR", flag: "🇶🇦", name: "ريال قطري" },
    "BHD": { rate: 0.98, symbol: "BHD", flag: "🇧🇭", name: "دينار بحريني" },
    "EGP": { rate: 125.50, symbol: "EGP", flag: "🇪🇬", name: "جنيه مصري" },
    "JOD": { rate: 1.84, symbol: "JOD", flag: "🇯🇴", name: "دينار أردني" },
    "IQD": { rate: 3405, symbol: "IQD", flag: "🇮🇶", name: "دينار عراقي" },
    "LYD": { rate: 12.60, symbol: "LYD", flag: "🇱🇾", name: "دينار ليبي" },
    "MAD": { rate: 26.20, symbol: "MAD", flag: "🇲🇦", name: "درهم مغربي" },
    "DZD": { rate: 348.50, symbol: "DZD", flag: "🇩🇿", name: "دينار جزائري" },
    "TND": { rate: 8.15, symbol: "TND", flag: "🇹🇳", name: "دينار تونسي" },
    "LBP": { rate: 232500, symbol: "LBP", flag: "🇱🇧", name: "ليرة لبنانية" },
    "SDG": { rate: 1560, symbol: "SDG", flag: "🇸🇩", name: "جنيه سوداني" },
    "YER": { rate: 650.50, symbol: "YER", flag: "🇾🇪", name: "ريال يمني" },
    "SYP": { rate: 33800, symbol: "SYP", flag: "🇸🇾", name: "ليرة سورية" },
    "MRU": { rate: 104.20, symbol: "MRU", flag: "🇲🇷", name: "أوقية موريتانية" },
    "DJF": { rate: 462.50, symbol: "DJF", flag: "🇩🇯", name: "فرنك جيبوتي" },
    "SOS": { rate: 1485, symbol: "SOS", flag: "🇸🇴", name: "شلن صومالي" },
    "KMF": { rate: 1180, symbol: "KMF", flag: "🇰🇲", name: "فرنك قمري" },
    "USD": { rate: 2.60, symbol: "$", flag: "🇺🇸", name: "دولار أمريكي" }
};

function initCurrencySwitcher() {
    const savedCurrency = localStorage.getItem('selectedCurrency') || 'OMR';
    const currencySelect = document.getElementById('globalCurrencySelector');
    if (currencySelect) {
        currencySelect.value = savedCurrency;
        currencySelect.addEventListener('change', function () {
            changeCurrency(this.value);
        });
    }
    changeCurrency(savedCurrency);
}

function changeCurrency(currencyCode) {
    localStorage.setItem('selectedCurrency', currencyCode);
    const rateData = currencyRates[currencyCode];
    if (!rateData) return;

    // تحديث كافة الأسعار التي تحمل كلاس currency-price
    document.querySelectorAll('.currency-price, .currency-old-price').forEach(el => {
        const basePrice = parseFloat(el.getAttribute('data-base-price'));
        if (!isNaN(basePrice)) {
            const converted = (basePrice * rateData.rate).toFixed(2);
            el.innerHTML = `${converted} ${rateData.symbol}`;
        }
    });
}

/* ===================================
   نظام المفضلة (Wishlist) والتنبيهات
   =================================== */

/**
 * تبديل حالة المنتج في المفضلة (إضافة/حذف)
 */
async function toggleWishlist(productId) {
    const btns = document.querySelectorAll(`.wishlist-btn[data-product-id="${productId}"], .btn-wishlist-pro[data-product-id="${productId}"]`);

    // إضافة تأثير نبض أثناء التحميل
    btns.forEach(b => b.classList.add('loading-pulse'));

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

        // === توجيه لصفحة تسجيل الدخول إذا تطلب الأمر ===
        if (result.redirectUrl) {
            window.location.href = result.redirectUrl;
            return;
        }

        if (result.success) {
            // إظهار تنبيه Premium
            showToast('success', result.message || getLocalizedString('addedToWishlist'));

            // تحديث حالة الأيقونة بصرياً
            btns.forEach(btn => {
                const icon = btn.querySelector('i');
                if (icon) {
                    if (icon.classList.contains('far')) {
                        icon.classList.replace('far', 'fas');
                        icon.classList.add('text-danger');
                        btn.classList.add('active');
                    } else {
                        icon.classList.replace('fas', 'far');
                        icon.classList.remove('text-danger');
                        btn.classList.remove('active');
                    }
                }
            });
        } else {
            showToast('error', result.message || getLocalizedString('errorOccurred'));
        }
    } catch (error) {
        console.error('Error toggling wishlist:', error);
        showToast('error', getLocalizedString('errorOccurred'));
    } finally {
        btns.forEach(b => b.classList.remove('loading-pulse'));
    }
}

/* ===================================
   إضافة منتج للسلة
   =================================== */

// جعل الدالة عالمية حتى تعمل مع onclick في الـ HTML 
window.addToCart = addToCartWithQty;

async function addToCartWithQty(productId, quantity = 1) {
    const qtyInput = document.getElementById('quantity');
    // إذا كانت الكمية محددة في الصفحة نستخدمها، وإلا نستخدم الافتراضي
    const finalQty = qtyInput ? parseInt(qtyInput.value) : quantity;

    try {
        const response = await fetch('/Cart/Add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ productId: parseInt(productId), quantity: finalQty })
        });

        const result = await response.json();

        // === التعديل هنا: توجيه المستخدم فوراً لصفحة الدخول إذا طلب السيرفر ذلك ===
        if (result.redirectUrl) {
            window.location.href = result.redirectUrl;
            return;
        }

        if (result.success) {
            showToast('success', getLocalizedString('addedToCart'));

            // تحديث العداد مباشرة من القيمة الراجعة من السيرفر
            document.querySelectorAll('.cart-count').forEach(el => {
                el.textContent = result.count;
                el.classList.add('bump'); // إضافة أنيميشن بسيط
                setTimeout(() => el.classList.remove('bump'), 300);
            });

        } else {
            showToast('error', result.message || getLocalizedString('errorOccurred'));
        }
    } catch (error) {
        console.error(error);
        showToast('error', getLocalizedString('errorOccurred'));
    }
}

/* ===================================
   أدوات مساعدة (Utils)
   =================================== */

function initToastSystem() {
    if (!document.getElementById('toastContainer')) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
}

function showToast(type, message, duration = 3500) {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
    toast.innerHTML = `<i class="fas ${icon}"></i><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease forwards';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

function updateCartCount() {
    fetch('/Cart/GetCartCount')
        .then(res => res.json())
        .then(data => {
            document.querySelectorAll('.cart-count').forEach(el => {
                el.textContent = data.count || 0;
            });
        }).catch(e => { });
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

function getLocalizedString(key) {
    const isAr = currentLanguage.startsWith('ar');
    const strings = {
        addedToCart: { en: 'Added to cart successfully', ar: 'تمت الإضافة للسلة بنجاح' },
        errorOccurred: { en: 'An error occurred', ar: 'حدث خطأ، يرجى المحاولة مرة أخرى' },
        addedToWishlist: { en: 'Wishlist updated', ar: 'تم تحديث قائمة المفضلة' }
    };
    return strings[key]?.[isAr ? 'ar' : 'en'] || key;
}

/* --- بقية وظائف التهيئة القياسية --- */

function initPreloader() {
    const preloader = document.getElementById('preloader');
    if (preloader) {
        window.addEventListener('load', () => setTimeout(() => preloader.classList.add('hidden'), 500));
    }
}

function initHeader() {
    const header = document.querySelector('.header');
    if (header) {
        window.addEventListener('scroll', () => {
            window.scrollY > 50 ? header.classList.add('scrolled') : header.classList.remove('scrolled');
        }, { passive: true });
    }
}

function initMobileMenu() {
    const btn = document.getElementById('mobileMenuBtn');
    const menu = document.getElementById('navMenu');
    if (btn && menu) {
        btn.addEventListener('click', () => {
            menu.classList.toggle('active');
            btn.querySelector('i').classList.toggle('fa-bars');
            btn.querySelector('i').classList.toggle('fa-times');
        });
    }
}

function initSearch() {
    const btn = document.getElementById('searchBtn');
    const overlay = document.getElementById('searchOverlay');
    if (btn && overlay) {
        btn.addEventListener('click', () => overlay.classList.add('active'));
        overlay.querySelector('#searchClose')?.addEventListener('click', () => overlay.classList.remove('active'));
    }
}

function initCart() {
    updateCartCount();
    document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
        // لتجنب تكرار الحدث إذا تم تعريفه سابقاً
        if (!btn.getAttribute('data-listener-attached')) {
            btn.setAttribute('data-listener-attached', 'true');
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                const id = btn.dataset.productId;
                if (id) addToCartWithQty(id);
            });
        }
    });
}

function initAnimations() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });
    document.querySelectorAll('.animate-on-scroll, .animate-fade-up').forEach(el => observer.observe(el));
}