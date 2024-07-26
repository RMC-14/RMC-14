using Content.Server._RMC14.Announce;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Popups;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private readonly List<string> _announce = [];

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var roundTime = _gameTicker.RoundDuration();
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            _announce.Clear();

            if (hive.NextConstructAllowed is {} buildCooldown && Timing.CurTime >= buildCooldown)
            {
                hive.NextConstructAllowed = null;
                Dirty(hiveId, hive);
                var msg = Loc.GetString("rmc-construction-cooldown-ended");
                _xenoAnnounce.AnnounceToHive(hiveId, msg, hive.AnnounceSound);
            }

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
            // below is commented out because it used to not do anything, now it works but isnt localized so i think its best to have a dedicated pr look at it
            //_xenoAnnounce.AnnounceTohive(hiveId, popup, hive.AnnounceSound, PopupType.Large);
        }
    }

    /// <summary>
    /// Create a new hive which gets networked to everyone for prediction.
    /// </summary>
    public EntityUid CreateHive(string name, EntProtoId? proto = null)
    {
        var ent = Spawn(proto ?? "CMXenoHive", MapCoordinates.Nullspace);
        _metaData.SetEntityName(ent, name);
        _pvsOverride.AddGlobalOverride(ent);
        return ent;
    }
}
