using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.Power;
using Content.Shared.Access.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Marines.GroundsideOperations;

public abstract class SharedGroundsideOperationsConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private TimeSpan _nextSummaryUpdate;
    private TimeSpan _summaryUpdateEvery = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, MapInitEvent>(OnMapInit);

        Subs.BuiEvents<GroundsideOperationsConsoleComponent>(GroundsideOperationsConsoleUi.Key, subs =>
        {
            subs.Event<GroundsideOperationsOpenOverwatchMsg>(OnOpenOverwatch);
            subs.Event<GroundsideOperationsHighCommandMsg>(OnHighCommand);
            subs.Event<GroundsideOperationsRedAlertMsg>(OnRedAlert);
            subs.Event<GroundsideOperationsGeneralQuartersMsg>(OnGeneralQuarters);
        });
    }

    private void OnBoundUiMessageAttempt(Entity<GroundsideOperationsConsoleComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        if (!_access.IsAllowed(args.Actor, ent.Owner))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-goc-access-denied"), args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_power.IsPowered(ent.Owner))
            return;

        args.Cancel();
        _popup.PopupClient(Loc.GetString("rmc-goc-unpowered"), args.Actor, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        if (!_net.IsServer || _timing.CurTime < _nextSummaryUpdate)
            return;

        _nextSummaryUpdate = _timing.CurTime + _summaryUpdateEvery;
        var consoles = EntityQueryEnumerator<GroundsideOperationsConsoleComponent, OverwatchConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var groundside, out var overwatch))
        {
            if (!_ui.IsUiOpen(uid, GroundsideOperationsConsoleUi.Key))
                continue;

            RefreshOverwatchSquads((uid, groundside), overwatch);
        }
    }

    private void OnMapInit(Entity<GroundsideOperationsConsoleComponent> ent, ref MapInitEvent args)
    {
        RefreshLandingZones(ent);
    }

    private void OnBoundUiOpened(Entity<GroundsideOperationsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_net.IsServer)
            return;

        RefreshLandingZones(ent);
        if (TryComp(ent, out OverwatchConsoleComponent? overwatch))
            RefreshOverwatchSquads(ent, overwatch);
    }

    private void OnOpenOverwatch(Entity<GroundsideOperationsConsoleComponent> ent, ref GroundsideOperationsOpenOverwatchMsg args)
    {
        if (_net.IsServer)
            _ui.TryOpenUi(ent.Owner, OverwatchConsoleUI.Key, args.Actor);
    }

    private void OnHighCommand(Entity<GroundsideOperationsConsoleComponent> ent, ref GroundsideOperationsHighCommandMsg args)
    {
        if (_net.IsServer)
            TrySendHighCommand(ent, args.Actor, args.Message);
    }

    private void OnRedAlert(Entity<GroundsideOperationsConsoleComponent> ent, ref GroundsideOperationsRedAlertMsg args)
    {
        if (_net.IsServer)
            TrySetRedAlert(ent, args.Actor);
    }

    private void OnGeneralQuarters(Entity<GroundsideOperationsConsoleComponent> ent, ref GroundsideOperationsGeneralQuartersMsg args)
    {
        if (_net.IsServer)
            TryCallGeneralQuarters(ent, args.Actor);
    }

    protected virtual void TrySendHighCommand(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor, string message)
    {
    }

    protected virtual void TrySetRedAlert(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor)
    {
    }

    protected virtual void TryCallGeneralQuarters(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor)
    {
    }

    private void RefreshLandingZones(Entity<GroundsideOperationsConsoleComponent> ent)
    {
        if (!_net.IsServer)
            return;

        var landingZones = new List<LandingZone>();
        foreach (var (id, metaData) in _dropship.GetPrimaryLZCandidates())
        {
            landingZones.Add(new LandingZone(GetNetEntity(id), metaData.EntityName));
        }

        landingZones.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        ent.Comp.LandingZones = landingZones;
        Dirty(ent);
    }

    private void RefreshOverwatchSquads(Entity<GroundsideOperationsConsoleComponent> ent, OverwatchConsoleComponent overwatch)
    {
        var summaries = new List<GroundsideOverwatchSquadSummary>();
        var squads = EntityQueryEnumerator<SquadTeamComponent>();
        while (squads.MoveNext(out var squadId, out var squad))
        {
            if (squad.Group != overwatch.Group)
                continue;

            var alive = 0;
            foreach (var squadMember in squad.Members)
            {
                if (TryComp(squadMember, out MobStateComponent? state) && state.CurrentState == MobState.Alive)
                    alive++;
            }

            string? leader = null;
            var leaders = EntityQueryEnumerator<SquadLeaderComponent, SquadMemberComponent>();
            while (leaders.MoveNext(out var leaderId, out _, out var squadMember))
            {
                if (squadMember.Squad == squadId)
                {
                    leader = Name(leaderId);
                    break;
                }
            }

            summaries.Add(new GroundsideOverwatchSquadSummary(
                GetNetEntity(squadId),
                Name(squadId),
                squad.Color,
                leader,
                squad.Members.Count,
                alive,
                squad.Objectives.GetValueOrDefault(SquadObjectiveType.Primary, string.Empty),
                squad.Objectives.GetValueOrDefault(SquadObjectiveType.Secondary, string.Empty)));
        }

        summaries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        if (ent.Comp.OverwatchSquads.SequenceEqual(summaries))
            return;

        ent.Comp.OverwatchSquads = summaries;
        Dirty(ent);
    }
}
