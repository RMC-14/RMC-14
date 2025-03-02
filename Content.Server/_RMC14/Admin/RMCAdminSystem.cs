using Content.Server._RMC14.TacticalMap;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly AdminSystem _admin = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public readonly Queue<(Guid Id, List<TacticalMapLine> Lines, string Actor, int Round)> LinesDrawn = new();

    private int _tacticalMapAdminHistorySize;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapUpdatedEvent>(OnTacticalMapUpdated);
        SubscribeLocalEvent<SpawnAsJobDialogEvent>(OnSpawnAsJobDialog);

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

    private void OnSpawnAsJobDialog(SpawnAsJobDialogEvent ev)
    {
        if (GetEntity(ev.User) is not { Valid: true } user)
            return;

        if (GetEntity(ev.Target) is not { Valid: true } target ||
            !TryComp(target, out ActorComponent? actor) ||
            !_transform.TryGetMapOrGridCoordinates(target, out var coords))
        {
            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), user, user);
            return;
        }

        var player = actor.PlayerSession;
        var stationUid = _station.GetOwningStation(target);
        var profile = _gameTicker.GetPlayerProfile(actor.PlayerSession);

        var newMind = _mind.CreateMind(player.UserId, profile.Name);
        _mind.SetUserId(newMind, player.UserId);
        _playTimeTracking.PlayerRolesChanged(player);
        var mobUid = _stationSpawning.SpawnPlayerCharacterOnStation(stationUid, ev.JobId, profile);

        _mind.TransferTo(newMind, mobUid);
        _role.MindAddJobRole(newMind, jobPrototype: ev.JobId);

        var jobName = _job.MindTryGetJobName(newMind);
        _admin.UpdatePlayerList(player);

        if (mobUid != null)
            _transform.SetCoordinates(mobUid.Value, coords.Value);

        _adminLog.Add(LogType.RMCSpawnJob, $"{ToPrettyString(user)} spawned {ToPrettyString(mobUid)} as job {jobName}");
    }

    protected override void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CanUse(player))
            return;

        _eui.OpenEui(new RMCAdminEui(target), player);
    }

    public EntityUid
        RandomizeMarine(EntityUid entity,
        ProtoId<SpeciesPrototype>? species = null,
        ProtoId<StartingGearPrototype>? gear = null,
        ProtoId<JobPrototype>? job = null)
    {
        var profile = species == null
            ? HumanoidCharacterProfile.Random()
            : HumanoidCharacterProfile.RandomWithSpecies(species);
        var coordinates = _transform.GetMoverCoordinates(entity);
        var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, job, profile, null);

        if (gear != null)
        {
            var startingGear = _prototypes.Index<StartingGearPrototype>(gear);
            _stationSpawning.EquipStartingGear(humanoid, startingGear);
        }

        return humanoid;
    }
}
