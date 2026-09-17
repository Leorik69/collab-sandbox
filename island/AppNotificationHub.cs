using System.Collections.Generic;
using System.Linq;

namespace NotifyIsland;

public sealed record AppToast(string Id, string Name, int Count, string? Aumid = null);

/// <summary>In-memory per-app unread. Fed by ToastNotificationListener or Demo.</summary>
internal static class AppNotificationHub
{
    private static readonly List<AppToast> Apps = new();

    public static int Total => Apps.Sum(x => x.Count);

    public static void Bump(string id, string? name = null)
    {
        var i = Apps.FindIndex(x => x.Id == id);
        if (i < 0) Apps.Add(new AppToast(id, name ?? id, 1));
        else Apps[i] = Apps[i] with { Count = Apps[i].Count + 1, Name = name ?? Apps[i].Name };
    }

    public static void Replace(IEnumerable<AppToast> next)
    {
        Apps.Clear();
        Apps.AddRange(next);
    }

    public static void Clear() => Apps.Clear();

    public static IReadOnlyList<AppToast> Snapshot() => Apps.ToList();
}
