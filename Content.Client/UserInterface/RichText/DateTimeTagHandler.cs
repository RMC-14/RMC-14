using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [date] tags into a button that fills in the current in-game date when clicked.
/// </summary>
public sealed class DateTimeTagHandler : PaperTimeStampTagHandler
{
    public override string Name => "date";

    protected override PaperTimeStampType Type => PaperTimeStampType.Date;

    protected override string ButtonText => Loc.GetString("paper-date-button");
}
