const CACHE_NAME = 'zegu-mrs-v2';

// Only genuinely static, rarely-changing vendor assets are pre-cached for
// offline fallback. App pages, site.css and site.js are NOT pre-cached here:
// they're cache-busted per deploy and must always be fetched fresh when the
// network is available.
const OFFLINE_FALLBACK_ASSETS = [
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap-icons/font/bootstrap-icons.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/favicon.ico',
  '/manifest.json'
];

self.addEventListener('install', function (event) {
  event.waitUntil(
    caches.open(CACHE_NAME).then(function (cache) {
      return cache.addAll(OFFLINE_FALLBACK_ASSETS);
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', function (event) {
  event.waitUntil(
    caches.keys()
      .then(function (cacheNames) {
        return Promise.all(
          cacheNames
            .filter(function (name) { return name !== CACHE_NAME; })
            .map(function (name) { return caches.delete(name); })
        );
      })
      .then(function () { return self.clients.claim(); })
  );
});

// Network-first for everything: this app serves authenticated, frequently
// changing pages (CSRF tokens, per-user data), so a stale cached page or
// asset must never win over a live network response. The cache is only a
// fallback for when the network request itself fails (offline).
self.addEventListener('fetch', function (event) {
  if (event.request.method !== 'GET') return;

  event.respondWith(
    fetch(event.request)
      .then(function (response) {
        if (response && response.status === 200 && response.type === 'basic') {
          var responseToCache = response.clone();
          caches.open(CACHE_NAME).then(function (cache) {
            cache.put(event.request, responseToCache);
          });
        }
        return response;
      })
      .catch(function () {
        return caches.match(event.request);
      })
  );
});
