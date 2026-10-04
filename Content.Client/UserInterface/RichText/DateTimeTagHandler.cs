using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Clock;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
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
        var entMan = IoCManager.Resolve<IEntityManager>();
        var ticker = entMan.System<SharedGameTicker>();
        var timeOffset = entMan.EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero;
        var dateOffset = entMan.EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.DateOffset ?? DateTime.Today.AddYears(100);
        var worldTime = timeOffset + ticker.RoundDuration();
        var worldDate = dateOffset + worldTime;
        return worldDate.ToString("dd/MM/yyyy");
    }

    public string TextAfter(MarkupNode node) => "";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        return false;
    }
}
