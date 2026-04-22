namespace Content.Shared._RMC14.UniversalRecorder;

public static class UniversalRecorderFormatting
{
    public static string FormatTimestamp(TimeSpan timestamp)
    {
        var totalMinutes = (int) timestamp.TotalMinutes;
        return $"[{totalMinutes:00}:{timestamp.Seconds:00}]";
    }

    public static string FormatTranscriptLine(TimeSpan timestamp, string speakerName, string text)
    {
        return FormatTranscriptLine(timestamp, speakerName, "says", text);
    }

    public static string FormatTranscriptLine(TimeSpan timestamp, string speakerName, string speechVerb, string text)
    {
        return $"{FormatTimestamp(timestamp)} {speakerName} {speechVerb}, \"{text}\"";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int) duration.TotalMinutes;
        return $"{totalMinutes}m {duration.Seconds}s";
    }
}
