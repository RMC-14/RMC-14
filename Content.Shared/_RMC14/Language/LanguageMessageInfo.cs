namespace Content.Shared._RMC14.Language;

public readonly struct LanguageMessageInfo
{
    public readonly string Message;
    public readonly string Language;
    public readonly Color? Color;
    public readonly string? Font;
    public readonly int? FontSize;

    public LanguageMessageInfo(string message, string language, Color? color = null, string? font = null, int? fontSize = null)
    {
        Message = message;
        Language = language;
        Color = color;
        Font = font;
        FontSize = fontSize;
    }
}
