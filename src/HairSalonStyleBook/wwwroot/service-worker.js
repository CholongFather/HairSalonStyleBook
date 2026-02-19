// 늘~오던 헤어살롱 - 공격적 이미지 캐싱 + 오프라인 폴백 Service Worker
const CACHE_NAME = 'salon-image-cache-v2';
const OFFLINE_CACHE = 'salon-offline-v1';
const OFFLINE_URL = 'offline.html';

// 캐싱할 이미지 패턴 (Firebase Storage + QR API)
const IMAGE_ORIGINS = [
    'firebasestorage.googleapis.com',
    'storage.googleapis.com',
    'always-hair-salon.firebasestorage.app',
    'api.qrserver.com'
];

const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.gif', '.svg', '.ico'];

// 이미지 요청인지 확인
function isImageRequest(url) {
    const urlLower = url.toLowerCase();

    // Firebase Storage / QR API 이미지
    if (IMAGE_ORIGINS.some(origin => urlLower.includes(origin))) {
        return true;
    }

    // 확장자 기반 이미지 판별
    if (IMAGE_EXTENSIONS.some(ext => urlLower.includes(ext))) {
        return true;
    }

    return false;
}

// 설치 - 오프라인 페이지 프리캐시 + 즉시 활성화
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(OFFLINE_CACHE).then(cache => cache.add(OFFLINE_URL))
    );
    self.skipWaiting();
});

// 활성화 - 오래된 캐시 정리 (현재 버전만 유지)
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(names =>
            Promise.all(
                names
                    .filter(name => name !== CACHE_NAME && name !== OFFLINE_CACHE)
                    .map(name => caches.delete(name))
            )
        ).then(() => self.clients.claim())
    );
});

// 네트워크 요청 가로채기 - 공격적 이미지 캐싱 + 오프라인 폴백
self.addEventListener('fetch', event => {
    const url = event.request.url;

    // 페이지 내비게이션 요청 - 오프라인 시 폴백 페이지 반환
    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request).catch(() =>
                caches.open(OFFLINE_CACHE).then(cache => cache.match(OFFLINE_URL))
            )
        );
        return;
    }

    // 이미지가 아니면 기본 동작
    if (!isImageRequest(url)) {
        return;
    }

    // Cache First 전략 (revalidation 없음 - Firebase URL은 토큰 포함 불변)
    event.respondWith(
        caches.open(CACHE_NAME).then(cache =>
            cache.match(event.request).then(cachedResponse => {
                if (cachedResponse) {
                    return cachedResponse;
                }

                // 캐시 미스 - 네트워크에서 가져와서 캐싱
                return fetch(event.request).then(networkResponse => {
                    if (networkResponse && networkResponse.ok) {
                        cache.put(event.request, networkResponse.clone());
                    }
                    return networkResponse;
                });
            })
        )
    );
});

// 캐시 크기 관리 - 최대 500개 이미지
self.addEventListener('message', event => {
    if (event.data === 'cleanup-cache') {
        caches.open(CACHE_NAME).then(cache =>
            cache.keys().then(keys => {
                if (keys.length > 500) {
                    // 오래된 항목부터 삭제
                    const deleteCount = keys.length - 400;
                    return Promise.all(
                        keys.slice(0, deleteCount).map(key => cache.delete(key))
                    );
                }
            })
        );
    }
});
