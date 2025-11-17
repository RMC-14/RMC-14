using Content.Server._RMC14.TacticalMap;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
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
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly AdminSystem _admin = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
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
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public readonly Queue<(Guid Id, List<TacticalMapLine> Lines, string Actor, int Round)> LinesDrawn = new();

    private int _tacticalMapAdminHistorySize;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapUpdatedEvent>(OnTacticalMapUpdated);
        SubscribeLocalEvent<SpawnAsJobDialogEvent>(OnSpawnAsJobDialog);
        _consoleHost.AnyCommandExecuted += ConsoleHostOnAnyCommandExecuted;

        Subs.CVar(_config, RMCCVars.RMCTacticalMapAdminHistorySize, v => _tacticalMapAdminHistorySize = v, true);
    }

    public override void Shutdown()
    {
        _consoleHost.AnyCommandExecuted -= ConsoleHostOnAnyCommandExecuted;
    }

    private void ConsoleHostOnAnyCommandExecuted(IConsoleShell shell, string commandName, string argStr, string[] args)
    {
        if (shell.IsLocal)
        {
            _adminLog.Add(LogType.RMCAdminCommandLogging, LogImpact.Medium, $"Server issued a command: {commandName}");
            return;
        }

        if (commandName.Contains("sudo"))
        {
            _adminLog.Add(LogType.RMCAdminCommandLogging, LogImpact.Medium, $"{shell.Player:player} issued a Sudo command with a command of {args[0]}.");
            return;
        }

        var log = "";
        foreach (var arg in args)
        {
            log += $" {arg}";
        }

        if (args.Length == 0)
        {
            log += " No arguments";
        }

        _adminLog.Add(LogType.RMCAdminCommandLogging, LogImpact.Medium, $"{shell.Player:player} issued command: {commandName} with arguments: {log}");
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
        if (GetEntity(ev.User) is not { Valid: true } user ||
            GetEntity(ev.Target) is not { Valid: true } target)
        {
            return;
        }

        SpawnAsJob(user, target, ev.JobId);
    }

    public void SpawnAsJob(EntityUid user, EntityUid target, ProtoId<JobPrototype> job)
    {
        if (!_adminManager.IsAdmin(user))
            return;

        if (!TryComp(target, out ActorComponent? actor) ||
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
        var mobUid = _stationSpawning.SpawnPlayerCharacterOnStation(stationUid, job, profile);

        _mind.TransferTo(newMind, mobUid);
        _role.MindAddJobRole(newMind, jobPrototype: job);

        var jobName = _job.MindTryGetJobName(newMind);
        _admin.UpdatePlayerList(player);

        if (mobUid != null)
        {
            EnsureComp<RMCAdminSpawnedComponent>(mobUid.Value);
            _transform.SetCoordinates(mobUid.Value, coords.Value);

            var spawnEv = new PlayerSpawnCompleteEvent(
                mobUid.Value,
                player,
                job,
                true,
                true,
                0,
                default,
                profile
            );
            RaiseLocalEvent(mobUid.Value, spawnEv, true);
        }

        if (HasComp<GhostComponent>(target))
        {
            RemComp<MindContainerComponent>(target);
            QueueDel(target);
        }

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
