using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.OrbitalCannon;
using Content.Shared._RMC14.Power;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.GroundsideOperations;

public abstract class SharedGroundsideOperationsConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly OrbitalCannonSystem _orbitalCannon = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PrimaryLandingZoneChangedEvent>(OnPrimaryLandingZoneChanged);
        SubscribeLocalEvent<OrbitalCannonChangedEvent>(OnOrbitalCannonChanged);
        SubscribeLocalEvent<OrbitalCannonLaunchEvent>(OnOrbitalCannonLaunch);
        SubscribeLocalEvent<OrbitalCannonSafetyChangedEvent>(OnOrbitalCannonSafetyChanged);

        Subs.BuiEvents<GroundsideOperationsConsoleComponent>(GroundsideOperationsConsoleUi.Key, subs =>
        {
            subs.Event<GroundsideOperationsHighCommandMsg>(OnHighCommand);
            subs.Event<GroundsideOperationsRedAlertMsg>(OnRedAlert);
            subs.Event<GroundsideOperationsGeneralQuartersMsg>(OnGeneralQuarters);
        });
    }

    private void OnBoundUiMessageAttempt(Entity<GroundsideOperationsConsoleComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        if (!_mobState.IsAlive(args.Actor))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-goc-invalid-user"), args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!_interaction.InRangeUnobstructed(args.Actor, ent.Owner))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-goc-out-of-range"), args.Actor, PopupType.MediumCaution);
            return;
        }

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

    protected virtual void OnMapInit(Entity<GroundsideOperationsConsoleComponent> ent, ref MapInitEvent args)
    {
        RefreshLandingZones(ent);
        RefreshOrdnance(ent);
    }

    protected virtual void OnBoundUiOpened(Entity<GroundsideOperationsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_net.IsServer)
            return;

        RefreshLandingZones(ent);
        RefreshOrdnance(ent);
    }

    private void OnPrimaryLandingZoneChanged(ref PrimaryLandingZoneChangedEvent args)
    {
        RefreshAllLandingZones();
    }

    private void OnHighCommand(Entity<GroundsideOperationsConsoleComponent> ent, ref GroundsideOperationsHighCommandMsg args)
    {
        if (_net.IsServer)
            TryOpenHighCommand(ent, args.Actor);
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

    protected virtual void TryOpenHighCommand(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor)
    {
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

    protected void RefreshLandingZones(Entity<GroundsideOperationsConsoleComponent> ent)
    {
        if (!_net.IsServer)
            return;

        string? primaryLandingZone = null;
        var primaryQuery = EntityQueryEnumerator<PrimaryLandingZoneComponent>();
        if (primaryQuery.MoveNext(out var primary, out _))
            primaryLandingZone = Name(primary);

        var landingZones = new List<LandingZone>();
        foreach (var (id, metaData) in _dropship.GetPrimaryLZCandidates())
        {
            landingZones.Add(new LandingZone(GetNetEntity(id), metaData.EntityName));
        }

        landingZones.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        ent.Comp.LandingZones = landingZones;
        ent.Comp.PrimaryLandingZone = primaryLandingZone;
        Dirty(ent);
    }

    public void RefreshAllLandingZones()
    {
        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<GroundsideOperationsConsoleComponent>();
        while (query.MoveNext(out var uid, out var groundside))
            RefreshLandingZones((uid, groundside));
    }

    protected void RefreshAllOrdnance()
    {
        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<GroundsideOperationsConsoleComponent>();
        while (query.MoveNext(out var uid, out var groundside))
            RefreshOrdnance((uid, groundside));
    }

    private void RefreshOrdnance(Entity<GroundsideOperationsConsoleComponent> ent)
    {
        if (!_net.IsServer)
            return;

        ent.Comp.OrbitalSafetyEngaged = _orbitalCannon.IsSafetyEngaged();
        if (!_orbitalCannon.TryGetClosestCannon(ent, out var cannon))
        {
            ent.Comp.HasOrbitalCannon = false;
            ent.Comp.OrbitalWarhead = null;
            ent.Comp.OrbitalFuel = 0;
            ent.Comp.OrbitalRequiredFuel = null;
            ent.Comp.OrbitalStatus = OrbitalCannonStatus.Unloaded;
            ent.Comp.NextOrbitalFire = default;
            Dirty(ent);
            return;
        }

        var status = _orbitalCannon.GetStatus(cannon);
        ent.Comp.HasOrbitalCannon = true;
        ent.Comp.OrbitalWarhead = status.Warhead;
        ent.Comp.OrbitalFuel = status.Fuel;
        ent.Comp.OrbitalRequiredFuel = status.RequiredFuel;
        ent.Comp.OrbitalStatus = status.Status;
        ent.Comp.NextOrbitalFire = status.NextFire;
        Dirty(ent);
    }

    private void OnOrbitalCannonChanged(ref OrbitalCannonChangedEvent ev)
    {
        RefreshAllOrdnance();
    }

    private void OnOrbitalCannonLaunch(ref OrbitalCannonLaunchEvent ev)
    {
        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<GroundsideOperationsConsoleComponent>();
        while (query.MoveNext(out var uid, out var groundside))
        {
            groundside.NextOrbitalFire = _timing.CurTime + ev.Cooldown;
            Dirty(uid, groundside);
        }
    }

    private void OnOrbitalCannonSafetyChanged(ref OrbitalCannonSafetyChangedEvent ev)
    {
        RefreshAllOrdnance();
    }
}
