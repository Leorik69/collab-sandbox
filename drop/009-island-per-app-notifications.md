# 009 — per-app notification island (P1)

Idle: compact pill, clock only.
Notification: morph, app icon + count.
Several apps: icon row, click opens that app's notifications.

This drop is the implementation sketch. `AppNotificationHub` is an in-memory stub until a Windows notification listener is wired (UserNotificationListener / notification-listener COM). Do not treat the stub as the real listener.

UX research: `internal/island-ux-research.md` (CC store) + `drop/008-island-ideal-ux.md`.
