# 009 — per-app notification island (P1)

Idle: compact pill, clock only.
Notification: morph, **app icon** + count.
Several apps: **AppIcons** row in the expanded pill; click opens Notification Center (that app’s toasts live there).

`AppNotificationHub` is the in-memory model. `ToastNotificationListener` wraps `UserNotificationListener` when Windows grants access; otherwise Demo +1 still feeds the hub.

Unpackaged hosts often get `Denied` — the island still works from the hub.
