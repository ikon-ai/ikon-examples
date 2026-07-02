/*
 * Ikon Web Push service worker — SCAFFOLD, NOT YET WIRED.
 *
 * Foreground notifications (app.Notifications) do NOT use this file — they are shown
 * directly via the Notification API while the client is connected. This service worker
 * exists only for OFFLINE Web Push, which is not enabled yet. It is intentionally NOT
 * registered anywhere; registering it has no effect until the backend push hub
 * (VAPID keys + subscription store + push sender) is in place.
 *
 * To enable Web Push later (see docs/private/ikon-notifications-guide.md):
 *   1. Register it on app start:  navigator.serviceWorker.register('/push-service-worker.js')
 *   2. Subscribe with the platform VAPID public key and POST the subscription to the
 *      Ikon backend push-hub endpoint, keyed by (spaceId, userId, deviceId).
 *   3. The backend sends Web Push messages; the handlers below render them.
 */

/* global self */

self.addEventListener('push', (event) => {
  if (!event.data) {
    return;
  }

  let payload = {};
  try {
    payload = event.data.json();
  } catch {
    payload = { title: event.data.text() };
  }

  const title = payload.title || 'Notification';
  const options = {
    body: payload.body,
    icon: payload.iconUrl,
    tag: payload.tag,
    data: { launchUrl: payload.launchUrl ?? null, data: payload.data ?? null },
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const launchUrl = event.notification.data && event.notification.data.launchUrl;

  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if ('focus' in client) {
          client.focus();
          if (launchUrl && 'navigate' in client) {
            client.navigate(launchUrl);
          }
          return undefined;
        }
      }
      if (launchUrl && self.clients.openWindow) {
        return self.clients.openWindow(launchUrl);
      }
      return undefined;
    }),
  );
});
