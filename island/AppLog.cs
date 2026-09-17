using System;
using System.IO;

namespace NotifyIsland;

internal static class AppLog
{
    public static void Write(string where, string? detail)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(path, $"[{DateTime.Now:O}] {where}\n{detail}\n\n");
        }
        catch
        {
            // ignore
        }
    }
}
