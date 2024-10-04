using Content.Server._RMC14.Announce;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private readonly List<string> _announce = [];

    public readonly EntProtoId DefaultHive = "CMXenoHive";

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var roundTime = _gameTicker.RoundDuration();
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

            var popup = Loc.GetString("rmc-hive-supports-castes", ("castes", string.Join(", ", _announce)));
            _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, popup, hive.AnnounceSound, PopupType.Large);
        }
    }

    /// <summary>
    /// Create a new hive with a name.
    /// </summary>
    public EntityUid CreateHive(string name, EntProtoId? proto = null)
    {
        var ent = Spawn(proto ?? DefaultHive);
        _metaData.SetEntityName(ent, name);
        return ent;
    }
}
