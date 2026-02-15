using Content.Server._RMC14.Commendations;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.Dogtags;
using Content.Shared.Database;
using System.Linq;

namespace Content.Server._RMC14.Rules;
/// <summary>
/// Contains Misc Functions for round end text appending, so it can be used across gamerules.
/// </summary>
public sealed class RMCGameRuleExtrasSystem : EntitySystem
{
    [Dependency] private readonly DogtagsSystem _dogtags = default!;
    [Dependency] private readonly CommendationSystem _commendation = default!;

    /// <summary>
    /// Shows names from memorials in the round end text. Returns true if there was any fallen listed.
    /// </summary>
    /// <param name="endEvent"></param>
    /// <returns></returns>
    public bool MemorialEntry(ref RoundEndTextAppendEvent endEvent)
    {
        var memorialQuery = EntityQueryEnumerator<RMCMemorialComponent>();
        List<string> fallen = new();

        while (memorialQuery.MoveNext(out var memorial))
        {
            fallen.AddRange(memorial.Names);
        }

        if (fallen.Count != 0)
        {
            string memorium = Loc.GetString("rmc-distress-signal-fallen", ("fallen", _dogtags.MemorialNamesFormat(fallen)));
            endEvent.AddLine(memorium);
            endEvent.AddLine(string.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Lists marines who were handed out medals. Returns true if there were any medals given.
    /// </summary>
    /// <param name="endEvent"></param>
    /// <returns></returns>
    public bool MarineAwards(ref RoundEndTextAppendEvent endEvent)
    {
        var commendations = _commendation.GetCommendations();
        var marineAwards = commendations.Where(c => c.Type == CommendationType.Medal).ToArray();
        if (marineAwards.Length > 0)
        {
            endEvent.AddLine(Loc.GetString("cm-distress-signal-medals"));
            foreach (var award in marineAwards)
            {
                endEvent.AddLine(Loc.GetString("rmc-distress-signal-got-medal", ("receiver", award.Receiver), ("award", award.Name),
                    ("awardDescription", award.Text), ("giver", award.Giver)));
            }

            endEvent.AddLine(string.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Lists xenos who were given a royal jelly. Returns true if there were any jellies given.
    /// </summary>
    /// <param name="endEvent"></param>
    /// <returns></returns>
    public bool XenoAwards(ref RoundEndTextAppendEvent endEvent)
    {
        var commendations = _commendation.GetCommendations();
        var xenoAwards = commendations.Where(c => c.Type == CommendationType.Jelly).ToArray();
        if (xenoAwards.Length > 0)
        {
            endEvent.AddLine(Loc.GetString("cm-distress-signal-jellies"));
            foreach (var award in xenoAwards)
            {
                endEvent.AddLine(Loc.GetString("rmc-distress-signal-got-jelly", ("receiver", award.Receiver), ("award", award.Name),
                    ("awardDescription", award.Text), ("giver", award.Giver)));
            }

            endEvent.AddLine(string.Empty);
            return true;
        }
        return false;
    }
}
