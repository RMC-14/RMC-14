using Content.Server._CM14.Announce;
using Content.Server.GameTicking;
using Content.Shared._CM14.Xenonids.Hive;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CM14.Xenonids.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<string> _announce = [];

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var roundTime = _timing.CurTime - _gameTicker.RoundStartTimeSpan;
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            _announce.Clear();

            for (var i = 0; i < hive.AnnouncementsLeft.Count; i++)
            {
                var left = hive.AnnouncementsLeft[i];
                if (roundTime >= left)
                {
                    if (hive.Unlocks.TryGetValue(left, out var unlocks))
                    {
                        foreach (var unlock in unlocks)
                        {
                            hive.AnnouncedUnlocks.Add(unlock);

                            if (_prototypes.TryIndex(unlock, out var prototype))
                            {
                                _announce.Add(prototype.Name);
                            }
                        }
                    }

                    hive.AnnouncementsLeft.RemoveAt(i);
                    i--;
                    Dirty(hiveId, hive);
                }
            }

            if (_announce.Count == 0)
                continue;

            var popup = $"The Hive can now support: {string.Join(", ", _announce)}";
            _xenoAnnounce.AnnounceSameHive(default, popup, hive.AnnounceSound, PopupType.Large);
        }
    }
}
