// 늘~오던 헤어살롱 - 이미지 캐싱 + 앱 셸 캐싱 + 오프라인 폴백 Service Worker
const IMAGE_CACHE = 'salon-image-cache-v2';
const STATIC_CACHE = 'salon-static-v1';
const OFFLINE_CACHE = 'salon-offline-v1';
const OFFLINE_URL = 'offline.html';

// 설치 시 프리캐시할 정적 자원 (앱 셸)
const PRECACHE_URLS = [
    './',
    'offline.html',
    'manifest.json',
    'favicon.svg',
    'icon-192.svg',
    'icon-512.svg',
    'css/app.css',
    'lib/bootstrap/dist/css/bootstrap.min.css',
    'lib/bootstrap/dist/js/bootstrap.bundle.min.js',
];

// 이미지 캐싱 대상 도메인
const IMAGE_ORIGINS = [
    'firebasestorage.googleapis.com',
    'storage.googleapis.com',
    'always-hair-salon.firebasestorage.app'
];

const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.gif', '.svg', '.ico'];

// 정적 자원 패턴 (stale-while-revalidate)
const STATIC_PATTERNS = ['.css', '.js', '.woff', '.woff2', '.ttf'];

function isImageRequest(url) {
    const urlLower = url.toLowerCase();
    if (IMAGE_ORIGINS.some(origin => urlLower.includes(origin))) return true;
    if (IMAGE_EXTENSIONS.some(ext => urlLower.includes(ext))) return true;
    return false;
}

function isStaticAsset(url) {
    const urlLower = url.toLowerCase();
    // 외부 CDN 폰트/CSS도 캐싱
    if (urlLower.includes('fonts.googleapis.com') || urlLower.includes('fonts.gstatic.com')) return true;
    if (urlLower.includes('cdn.jsdelivr.net')) return true;
    // 로컬 정적 자원
    if (STATIC_PATTERNS.some(ext => urlLower.includes(ext))) return true;
    return false;
}

// 설치 - 앱 셸 프리캐시 + 즉시 활성화
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then(cache => cache.addAll(PRECACHE_URLS))
            .catch(err => console.log('[SW] 프리캐시 일부 실패:', err))
    );
    self.skipWaiting();
});

// 활성화 - 오래된 캐시 정리
self.addEventListener('activate', event => {
    const keepCaches = [IMAGE_CACHE, STATIC_CACHE, OFFLINE_CACHE];
    event.waitUntil(
        caches.keys().then(names =>
            Promise.all(
                names
                    .filter(name => !keepCaches.includes(name))
                    .map(name => caches.delete(name))
            )
        ).then(() => self.clients.claim())
    );
});

// 네트워크 요청 가로채기
self.addEventListener('fetch', event => {
    const url = event.request.url;

    // POST 등 GET이 아닌 요청은 무시
    if (event.request.method !== 'GET') return;

    // Firestore API 요청은 캐싱하지 않음
    if (url.includes('firestore.googleapis.com')) return;

    // 1. 페이지 내비게이션 - Network First + 오프라인 폴백
    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    // 성공하면 캐시에도 저장
                    const clone = response.clone();
                    caches.open(STATIC_CACHE).then(cache => cache.put(event.request, clone));
                    return response;
                })
                .catch(() =>
                    caches.match(event.request)
                        .then(cached => cached || caches.open(OFFLINE_CACHE).then(c => c.match(OFFLINE_URL)))
                )
        );
        return;
    }

    // 2. 이미지 - Cache First (Firebase URL은 토큰 포함 불변)
    if (isImageRequest(url)) {
        event.respondWith(
            caches.open(IMAGE_CACHE).then(cache =>
                cache.match(event.request).then(cached => {
                    if (cached) return cached;
                    return fetch(event.request).then(response => {
                        if (response && response.ok) {
                            cache.put(event.request, response.clone());
                        }
                        return response;
                    });
                })
            )
        );
        return;
    }

    // 3. 정적 자원 (CSS, JS, 폰트) - Stale While Revalidate
    if (isStaticAsset(url)) {
        event.respondWith(
            caches.open(STATIC_CACHE).then(cache =>
                cache.match(event.request).then(cached => {
                    const fetchPromise = fetch(event.request).then(response => {
                        if (response && response.ok) {
                            cache.put(event.request, response.clone());
                        }
                        return response;
                    }).catch(() => cached);

                    return cached || fetchPromise;
                })
            )
        );
        return;
    }
});

// 캐시 크기 관리 - 최대 500개 이미지
self.addEventListener('message', event => {
    if (event.data === 'cleanup-cache') {
        caches.open(IMAGE_CACHE).then(cache =>
            cache.keys().then(keys => {
                if (keys.length > 500) {
                    const deleteCount = keys.length - 400;
                    return Promise.all(
                        keys.slice(0, deleteCount).map(key => cache.delete(key))
                    );
                }
            })
        );
    }
});
