using System.Diagnostics.CodeAnalysis;
using Content.Client._RMC14.Rules;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed class OperationTagHandler : IMarkupTagHandler
{
    public string Name => "operation";

    public bool CanHandle(MarkupNode node) => node.Name == "operation";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }

    public string TextBefore(MarkupNode node)
    {
        var name = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RMCRoundInfoSystem>().GetOperationName();
        return string.IsNullOrEmpty(name) ? "Unknown Operation" : name;
    }

    public string TextAfter(MarkupNode node) => "";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        return false;
    }
}
