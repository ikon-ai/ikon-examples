/*
 * Web Push service worker: shows notifications that arrive while the app is
 * closed, and focuses (or opens) the app when one is clicked.
 *
 * The SDK registers this file for you once the user grants notification
 * permission; notifications shown while the app is open never reach it. Edit the
 * handlers below to change how offline notifications look or where a click lands.
 */

/* global self */

self.addEventListener('push', (event) => {
  if (!event.data) {
    return;
  }

  let payload;
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
