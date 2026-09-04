// ZEGU MRS - Mobile & UX Enhancements

(function() {
    'use strict';

    // Auto-dismiss alerts after 5 seconds
    document.addEventListener('DOMContentLoaded', function() {
        const alerts = document.querySelectorAll('.alert.alert-success, .alert.alert-info');
        alerts.forEach(function(alert) {
            setTimeout(function() {
                if (alert && alert.parentNode) {
                    const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                    bsAlert.close();
                }
            }, 5000);
        });
    });

    // Add data-label to table cells for mobile card view if not present
    document.addEventListener('DOMContentLoaded', function() {
        const tables = document.querySelectorAll('.table-mobile-cards');
        tables.forEach(function(table) {
            const headers = table.querySelectorAll('thead th');
            const headerTexts = Array.from(headers).map(function(th) { return th.textContent.trim(); });

            const rows = table.querySelectorAll('tbody tr');
            rows.forEach(function(row) {
                const cells = row.querySelectorAll('td');
                cells.forEach(function(cell, index) {
                    if (!cell.hasAttribute('data-label') && headerTexts[index]) {
                        cell.setAttribute('data-label', headerTexts[index]);
                    }
                });
            });
        });
    });

    // Confirm dangerous actions
    document.addEventListener('submit', function(e) {
        const form = e.target;
        const submitBtn = form.querySelector('button[type="submit"][data-confirm]');
        if (submitBtn) {
            const message = submitBtn.getAttribute('data-confirm');
            if (!confirm(message)) {
                e.preventDefault();
            }
        }
    });

    // Touch-friendly: prevent double-tap zoom on buttons
    document.addEventListener('touchstart', function(e) {
        if (e.target.tagName === 'BUTTON' || e.target.tagName === 'A') {
            e.target.style.touchAction = 'manipulation';
        }
    }, { passive: true });

    // Show loading indicator on slow requests
    let loadingTimeout;
    document.addEventListener('submit', function(e) {
        const form = e.target;
        if (form.tagName === 'FORM' && !form.classList.contains('no-loader')) {
            loadingTimeout = setTimeout(function() {
                showLoading();
            }, 1000);
        }
    });

    window.addEventListener('pageshow', function() {
        hideLoading();
        if (loadingTimeout) {
            clearTimeout(loadingTimeout);
            loadingTimeout = null;
        }
    });

    function showLoading() {
        let overlay = document.getElementById('globalLoadingOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'globalLoadingOverlay';
            overlay.className = 'spinner-overlay';
            overlay.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>';
            document.body.appendChild(overlay);
        }
        setTimeout(function() { overlay.classList.add('show'); }, 10);
    }

    function hideLoading() {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.remove('show');
        }
    }

    // Form validation feedback
    document.addEventListener('invalid', function(e) {
        const target = e.target;
        if (target.classList) {
            target.classList.add('is-invalid');
        }
    }, true);

    document.addEventListener('input', function(e) {
        if (e.target.classList && e.target.classList.contains('is-invalid')) {
            e.target.classList.remove('is-invalid');
        }
    });

    // Image lazy loading fallback
    if ('IntersectionObserver' in window) {
        const lazyImages = document.querySelectorAll('img[data-src]');
        const imageObserver = new IntersectionObserver(function(entries, observer) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                    imageObserver.unobserve(img);
                }
            });
        });
        lazyImages.forEach(function(img) { imageObserver.observe(img); });
    }

    // Register service worker for PWA
    if ('serviceWorker' in navigator && window.location.protocol !== 'file:') {
        window.addEventListener('load', function() {
            navigator.serviceWorker.register('/sw.js').then(function(reg) {
                console.log('ServiceWorker registered:', reg.scope);
            }).catch(function(err) {
                console.log('ServiceWorker registration failed:', err);
            });
        });
    }

    // Pull-to-refresh for mobile (basic implementation)
    let pullStartY = 0;
    let pullCurrentY = 0;
    let isPulling = false;

    document.addEventListener('touchstart', function(e) {
        if (window.scrollY === 0 && e.touches.length === 1) {
            pullStartY = e.touches[0].clientY;
            isPulling = true;
        }
    }, { passive: true });

    document.addEventListener('touchmove', function(e) {
        if (!isPulling) return;
        pullCurrentY = e.touches[0].clientY;
    }, { passive: true });

    document.addEventListener('touchend', function() {
        if (isPulling && pullCurrentY - pullStartY > 100) {
            // Trigger refresh
            window.location.reload();
        }
        isPulling = false;
        pullStartY = 0;
        pullCurrentY = 0;
    });

    // Online/offline status indicator
    window.addEventListener('online', function() {
        showToast('You are back online', 'success');
    });

    window.addEventListener('offline', function() {
        showToast('You are offline. Some features may be limited.', 'warning');
    });

    function showToast(message, type) {
        const toast = document.createElement('div');
        toast.className = 'toast-notification toast-' + type;
        toast.innerHTML = '<i class="bi bi-' + (type === 'success' ? 'check-circle' : 'exclamation-triangle') + '"></i> ' + message;
        toast.style.cssText = 'position:fixed;top:20px;right:20px;left:20px;max-width:400px;margin:0 auto;background:' + (type === 'success' ? '#198754' : '#ffc107') + ';color:' + (type === 'success' ? 'white' : '#212529') + ';padding:12px 16px;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.15);z-index:9999;display:flex;align-items:center;gap:8px;font-size:0.9rem;animation:slideUp 0.3s ease;';
        document.body.appendChild(toast);
        setTimeout(function() {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.3s';
            setTimeout(function() { toast.remove(); }, 300);
        }, 3000);
    }
})();
