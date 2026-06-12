namespace Content.Server._RMC14.Announce.Core;

internal static class AnnouncementLineHelper
{
    internal static string[] NormalizeAndSplit(string message)
    {
        if (string.IsNullOrEmpty(message))
            return Array.Empty<string>();

        var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.Contains('\n') && normalized.Contains("\\n"))
            normalized = normalized.Replace("\\n", "\n");

        return normalized.Split('\n');
    }
}
