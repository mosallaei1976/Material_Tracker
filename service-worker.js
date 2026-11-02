/* Manifest version: YIPIKSy4 */
// The SDK generates a versioned list containing the complete Blazor runtime.
importScripts('./service-worker-assets.js');

const appBase = new URL('./', self.registration.scope);
const cachePrefix = `sitepack-static:${appBase.pathname}:`;
const cacheName = cachePrefix + self.assetsManifest.version;
const shellUrl = new URL('./index.html', appBase).href;
const assets = self.assetsManifest.assets.filter(asset =>
    !/^(?:service-worker(?:-assets)?\.js|404\.html)$/.test(asset.url));
const assetUrls = new Set(assets.map(asset => new URL(asset.url, appBase).href));

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(cacheName);
        await cache.addAll(assets.map(asset => new Request(new URL(asset.url, appBase), {
            cache: 'reload',
            // GitHub Pages changes the base href after publishing, so the
            // generated index.html hash no longer describes the deployed HTML.
            integrity: asset.url === 'index.html' ? '' : asset.hash
        })));
        // Let existing tabs finish using their own version before activating.
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.filter(key => key.startsWith(cachePrefix) && key !== cacheName)
            .map(key => caches.delete(key)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);
    if (request.method !== 'GET' || url.origin !== appBase.origin ||
        !url.pathname.startsWith(appBase.pathname)) {
        return;
    }

    // Cache only the app shell and known static assets. Browser localStorage
    // and downloaded user backups are independent of this cache.
    const cacheKey = request.mode === 'navigate' ? shellUrl : request.url;
    if (request.mode !== 'navigate' && !assetUrls.has(cacheKey)) {
        return;
    }

    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        return await cache.match(cacheKey) || fetch(request);
    })());
});
