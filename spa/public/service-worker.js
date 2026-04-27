// Skeleton service worker — registered solely so Android Chrome considers
// the app installable (it requires an SW with a fetch handler). Intentionally
// does NOT cache or alter responses.

self.addEventListener('install', () => {
  self.skipWaiting()
})

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim())
})

self.addEventListener('fetch', () => {
  // No-op: let the browser handle the request as normal.
})
