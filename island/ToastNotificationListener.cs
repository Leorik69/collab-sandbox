using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotifyIsland;

/// <summary>Best-effort toast listener. Fails closed to AppNotificationHub/Demo.</summary>
internal static class ToastNotificationListener
{
    public static bool IsListening { get; private set; }

    public static async Task StartAsync(Action onChanged)
    {
        try
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
                return;
            var listener = UserNotificationListener.Current;
            var access = await listener.RequestAccessAsync();
            if (access != UserNotificationListenerAccessStatus.Allowed)
                return;
            await PullAsync(listener);
            listener.NotificationChanged += (_, _) =>
            {
                _ = PullAsync(listener).ContinueWith(_ => onChanged());
            };
            IsListening = true;
            onChanged();
        }
        catch
        {
            IsListening = false;
        }
    }

    private static async Task PullAsync(UserNotificationListener listener)
    {
        try
        {
            var toasts = await listener.GetNotificationsAsync(NotificationKinds.Toast);
            var groups = new Dictionary<string, AppToast>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in toasts)
            {
                string id = "app";
                string name = "App";
                string? aumid = null;
                try
                {
                    var info = n.AppInfo;
                    if (info != null)
                    {
                        aumid = info.AppUserModelId;
                        id = aumid ?? info.DisplayInfo.DisplayName;
                        name = info.DisplayInfo.DisplayName;
                    }
                }
                catch { /* AppInfo can throw for some payloads */ }
                if (groups.TryGetValue(id, out var cur))
                    groups[id] = cur with { Count = cur.Count + 1 };
                else
                    groups[id] = new AppToast(id, name, 1, aumid);
            }
            AppNotificationHub.Replace(groups.Values);
        }
        catch
        {
            // keep last snapshot
        }
    }
}
