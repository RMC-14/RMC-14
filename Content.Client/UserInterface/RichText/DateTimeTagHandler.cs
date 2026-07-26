using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed class DateTimeTagHandler : IMarkupTagHandler
{
    public string Name => "date";

    public bool CanHandle(MarkupNode node) => node.Name == "date";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }

    public string TextBefore(MarkupNode node)
    {
        return DateTime.UtcNow.AddYears(100).ToString("dd/MM/yyyy");
    }

    public string TextAfter(MarkupNode node) => "";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        return false;
    }
}
