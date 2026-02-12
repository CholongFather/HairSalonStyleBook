// 늘~오던 헤어살롱 - 이미지 캐싱 Service Worker
const CACHE_NAME = 'salon-image-cache-v1';

// 캐싱할 이미지 패턴
const IMAGE_ORIGINS = [
    'firebasestorage.googleapis.com',
    'storage.googleapis.com',
    'always-hair-salon.firebasestorage.app'
];

const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.gif', '.svg', '.ico'];

// 이미지 요청인지 확인
function isImageRequest(url) {
    const urlLower = url.toLowerCase();

    // Firebase Storage 이미지
    if (IMAGE_ORIGINS.some(origin => urlLower.includes(origin))) {
        return true;
    }

    // 확장자 기반 이미지 판별
    if (IMAGE_EXTENSIONS.some(ext => urlLower.includes(ext))) {
        return true;
    }

    return false;
}

// 설치
self.addEventListener('install', event => {
    self.skipWaiting();
});

// 활성화 - 오래된 캐시 정리
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(names =>
            Promise.all(
                names
                    .filter(name => name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            )
        ).then(() => self.clients.claim())
    );
});

// 네트워크 요청 가로채기 - 이미지만 캐싱
self.addEventListener('fetch', event => {
    const url = event.request.url;

    // 이미지가 아니면 기본 동작
    if (!isImageRequest(url)) {
        return;
    }

    // Cache First 전략: 캐시에 있으면 즉시 반환, 없으면 네트워크
    event.respondWith(
        caches.open(CACHE_NAME).then(cache =>
            cache.match(event.request).then(cachedResponse => {
                if (cachedResponse) {
                    // 캐시 히트 - 백그라운드에서 새 버전 업데이트 (stale-while-revalidate)
                    const fetchPromise = fetch(event.request).then(networkResponse => {
                        if (networkResponse && networkResponse.ok) {
                            cache.put(event.request, networkResponse.clone());
                        }
                        return networkResponse;
                    }).catch(() => { /* 오프라인이면 무시 */ });

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

// 캐시 크기 관리 - 최대 200개 이미지
self.addEventListener('message', event => {
    if (event.data === 'cleanup-cache') {
        caches.open(CACHE_NAME).then(cache =>
            cache.keys().then(keys => {
                if (keys.length > 200) {
                    // 오래된 항목부터 삭제
                    const deleteCount = keys.length - 150;
                    return Promise.all(
                        keys.slice(0, deleteCount).map(key => cache.delete(key))
                    );
                }
            })
        );
    }
});
