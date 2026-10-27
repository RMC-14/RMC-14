using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [time] tags into a button that fills in the current in-game time when clicked.
/// </summary>
public sealed class TimeTagHandler : PaperTimeStampTagHandler
{
    public override string Name => "time";

    protected override PaperTimeStampType Type => PaperTimeStampType.Time;

    protected override string ButtonText => Loc.GetString("paper-time-button");
}
