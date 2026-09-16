using System.Collections.Generic;
using System.Linq;

namespace NotifyIsland;

/// <summary>P1 stub: per-app unread until a real Windows notification listener lands.</summary>
internal static class AppNotificationHub
{
    public static readonly List<(string App, int Count)> Apps = new();

    public static void Bump(string app)
    {
        var i = Apps.FindIndex(x => x.App == app);
        if (i < 0) Apps.Add((app, 1));
        else Apps[i] = (app, Apps[i].Count + 1);
    }

    public static void Clear() => Apps.Clear();

    public static IReadOnlyList<(string App, int Count)> Snapshot() => Apps.ToList();
}
