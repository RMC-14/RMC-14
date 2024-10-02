using Content.Server._RMC14.TacticalMap;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public readonly Queue<(Guid Id, List<TacticalMapLine> Lines, string Actor, int Round)> LinesDrawn = new();

    private int _tacticalMapAdminHistorySize;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapUpdatedEvent>(OnTacticalMapUpdated);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapAdminHistorySize, v => _tacticalMapAdminHistorySize = v, true);
    }

    private void OnTacticalMapUpdated(ref TacticalMapUpdatedEvent ev)
    {
        LinesDrawn.Enqueue((Guid.NewGuid(), ev.Lines, ToPrettyString(ev.Actor), _gameTicker.RoundId));

        while (LinesDrawn.Count > 0 && LinesDrawn.Count > _tacticalMapAdminHistorySize)
        {
            LinesDrawn.Dequeue();
        }
    }

    protected override void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CanUse(player))
            return;

        _eui.OpenEui(new RMCAdminEui(target), player);
    }

    public EntityUid RandomizeMarine(EntityUid entity,
        ProtoId<SpeciesPrototype>? species = null,
        ProtoId<StartingGearPrototype>? gear = null,
        ProtoId<JobPrototype>? job = null)
    {
        var profile = species == null
            ? HumanoidCharacterProfile.Random()
            : HumanoidCharacterProfile.RandomWithSpecies(species);
        var coordinates = _transform.GetMoverCoordinates(entity);
        var jobComp = job == null ? null : new JobComponent { Prototype = job.Value };
        var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, jobComp, profile, null);

        if (gear != null)
        {
            var startingGear = _prototypes.Index<StartingGearPrototype>(gear);
            _stationSpawning.EquipStartingGear(humanoid, startingGear);
        }

        return humanoid;
    }
}
