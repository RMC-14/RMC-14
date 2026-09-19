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

        var lines = normalized.Split('\n');
        var firstNonBlank = Array.FindIndex(lines, l => !string.IsNullOrWhiteSpace(l));
        if (firstNonBlank < 0)
            return Array.Empty<string>();

        var lastNonBlank = Array.FindLastIndex(lines, l => !string.IsNullOrWhiteSpace(l));
        return lines[firstNonBlank..(lastNonBlank + 1)];
    }
}
