// HairSalonStyleBook - 공용 JS 유틸리티 모듈
// eval 인라인 호출을 대체하는 네임스페이스 함수들

window.salonApp = {
    // === 유틸리티 ===

    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    getWindowWidth: function () {
        return window.innerWidth;
    },

    getScreenSize: function () {
        return window.screen.width + 'x' + window.screen.height;
    },

    isStandalonePWA: function () {
        return window.matchMedia('(display-mode: standalone)').matches;
    },

    isIOS: function () {
        return (/iPhone|iPad|iPod/i).test(navigator.userAgent);
    },

    copyToClipboard: function (text) {
        return navigator.clipboard.writeText(text);
    },

    // === 맨 위로 버튼 + 비활성 자동 리셋 (MainLayout) ===

    initScrollAndIdle: function () {
        // 맨 위로 버튼 스크롤 감지
        var btn = document.querySelector('.btn-scroll-top');
        if (btn) {
            window.addEventListener('scroll', function () {
                if (window.scrollY > 300) btn.classList.add('visible');
                else btn.classList.remove('visible');
            }, { passive: true });
        }

        // 비활성 자동 리셋 (5분)
        var IDLE_TIMEOUT = 5 * 60 * 1000;
        var idleTimer = null;
        var basePath = document.querySelector('base')?.getAttribute('href') || '/';
        function resetIdleTimer() {
            clearTimeout(idleTimer);
            idleTimer = setTimeout(function () {
                if (window.location.pathname.indexOf('/login') !== -1) return;
                if (window.location.pathname.indexOf('/admin') !== -1) {
                    localStorage.removeItem('salon_auth');
                    localStorage.removeItem('salon_role');
                    window.location.href = basePath + 'login';
                } else {
                    window.scrollTo(0, 0);
                    window.location.href = basePath;
                }
            }, IDLE_TIMEOUT);
        }
        ['touchstart', 'touchmove', 'click', 'scroll', 'keydown'].forEach(function (evt) {
            document.addEventListener(evt, resetIdleTimer, { passive: true });
        });
        resetIdleTimer();
    },

    // === ImageCarousel ===

    initCarousel: function (carouselId) {
        var el = document.getElementById(carouselId);
        if (!el) return;
        new bootstrap.Carousel(el, { touch: true, interval: false });

        // 슬라이드 카운터 업데이트
        el.addEventListener('slid.bs.carousel', function (e) {
            var counter = el.querySelector('.carousel-counter-current');
            if (counter) counter.textContent = e.to + 1;
        });

        // 커스텀 터치 스와이프
        var startX = 0, startY = 0, moving = false;
        el.addEventListener('touchstart', function (e) {
            startX = e.touches[0].clientX;
            startY = e.touches[0].clientY;
            moving = true;
        }, { passive: true });
        el.addEventListener('touchend', function (e) {
            if (!moving) return;
            moving = false;
            var diffX = e.changedTouches[0].clientX - startX;
            var diffY = e.changedTouches[0].clientY - startY;
            if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > 40) {
                var inst = bootstrap.Carousel.getInstance(el);
                if (diffX < 0) inst.next();
                else inst.prev();
            }
        }, { passive: true });
    },

    disposeCarousel: function (carouselId) {
        var el = document.getElementById(carouselId);
        if (el) {
            var inst = bootstrap.Carousel.getInstance(el);
            if (inst) inst.dispose();
        }
    },

    // === RichTextEditor ===

    richEditor: {
        setHtml: function (editorId, html) {
            var el = document.getElementById(editorId);
            if (el) el.innerHTML = html;
        },

        getHtml: function (editorId) {
            var el = document.getElementById(editorId);
            return el ? el.innerHTML : '';
        },

        execCommand: function (editorId, command, value) {
            var el = document.getElementById(editorId);
            if (el) {
                el.focus();
                document.execCommand(command, false, value || null);
            }
        },

        insertText: function (editorId, text) {
            var el = document.getElementById(editorId);
            if (el) {
                el.focus();
                document.execCommand('insertText', false, text);
            }
        }
    },

    // === Login 페이지 ===

    getDeviceFingerprint: function () {
        var ua = navigator.userAgent;
        var screen = window.screen.width + 'x' + window.screen.height;
        var tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
        var lang = navigator.language;
        var raw = ua + '|' + screen + '|' + tz + '|' + lang;
        // FNV-1a 해시
        var hash = 0x811c9dc5;
        for (var i = 0; i < raw.length; i++) {
            hash ^= raw.charCodeAt(i);
            hash = Math.imul(hash, 0x01000193);
        }
        return (hash >>> 0).toString(16).padStart(8, '0');
    },

    getDeviceInfo: function () {
        var ua = navigator.userAgent;
        if (ua.includes('iPhone')) return 'iPhone';
        if (ua.includes('iPad')) return 'iPad';
        if (ua.includes('Android')) return 'Android';
        if (ua.includes('Windows')) return 'Windows';
        if (ua.includes('Mac')) return 'Mac';
        return 'Unknown';
    },

    // === Pay 페이지 ===

    payTimer: {
        _lastActivity: 0,

        init: function () {
            this._lastActivity = Date.now();
            var self = this;
            ['touchstart', 'click', 'scroll', 'keydown', 'keyup'].forEach(function (evt) {
                document.addEventListener(evt, function () { self._lastActivity = Date.now(); }, { passive: true });
            });
        },

        getElapsedMs: function () {
            return Date.now() - this._lastActivity;
        }
    },

    tryOpenApp: function (scheme, fallback) {
        var w = window.open(scheme, '_self');
        setTimeout(function () {
            if (document.hasFocus()) window.location.href = fallback;
        }, 1500);
    },

    closePage: function () {
        window.close();
        setTimeout(function () {
            history.replaceState(null, '', location.href);
            history.pushState(null, '', location.href);
            window.addEventListener('popstate', function () {
                history.pushState(null, '', location.href);
            });
        }, 100);
    },

    // === Dashboard ===

    fetchImageAsBase64: function (url) {
        return fetch(url)
            .then(function (r) { return r.blob(); })
            .then(function (b) {
                return new Promise(function (ok, fail) {
                    var r = new FileReader();
                    r.onload = function () { ok(r.result.split(',')[1]); };
                    r.onerror = fail;
                    r.readAsDataURL(b);
                });
            });
    }
};
