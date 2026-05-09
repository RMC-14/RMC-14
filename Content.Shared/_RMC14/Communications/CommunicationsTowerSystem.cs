using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.ManageHive.Boons;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Communications;

public sealed class CommunicationsTowerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HiveBoonSystem _hiveBoon = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntProtoId, List<Entity<CommunicationsTowerSpawnerComponent>>> _spawners = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CommunicationsTowerComponent, MapInitEvent>(OnTowerMapInit);
        SubscribeLocalEvent<CommunicationsTowerComponent, DamageChangedEvent>(OnTowerDamageChanged);
        SubscribeLocalEvent<CommunicationsTowerComponent, BreakageEventArgs>(OnTowerBreakage);
        SubscribeLocalEvent<CommunicationsTowerComponent, ExaminedEvent>(OnTowerExamined);
        SubscribeLocalEvent<CommunicationsTowerComponent, InteractUsingEvent>(OnTowerInteractUsing);
        SubscribeLocalEvent<CommunicationsTowerComponent, DialogChosenEvent>(OnTowerDialogChosen);
        SubscribeLocalEvent<CommunicationsTowerComponent, CommunicationsTowerWipeDoAfterEvent>(OnTowerDialogWipeDoAfter);
        SubscribeLocalEvent<CommunicationsTowerComponent, CommunicationsTowerAddDoAfterEvent>(OnTowerDialogAddDoAfter);
        SubscribeLocalEvent<CommunicationsTowerComponent, InteractHandEvent>(OnTowerInteractHand);
        SubscribeLocalEvent<CommunicationsTowerComponent, PowerChangedEvent>(OnTowerPowerChangedEvent);
    }

    private void OnTowerMapInit(Entity<CommunicationsTowerComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnTowerDamageChanged(Entity<CommunicationsTowerComponent> ent, ref DamageChangedEvent args)
    {
        if (args.Damageable.TotalDamage > FixedPoint2.Zero)
            return;

        if (ent.Comp.State != CommunicationsTowerState.Broken)
            return;

        ChangeState(ent, CommunicationsTowerState.Off);
    }

    private void OnTowerBreakage(Entity<CommunicationsTowerComponent> ent, ref BreakageEventArgs args)
    {
        ChangeState(ent, CommunicationsTowerState.Broken);
    }

    private void OnTowerExamined(Entity<CommunicationsTowerComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CommunicationsTowerComponent)))
        {
            var msg = Loc.GetString("rmc-comm-tower-cluster-pylon-minutes", ("minutes", (int) _hiveBoon.CommunicationTowerXenoTakeoverTime.TotalMinutes));
            args.PushMarkup(msg);
            if (ent.Comp.State != CommunicationsTowerState.Broken)
                return;

            args.PushMarkup(Loc.GetString("rmc-comm-tower-damaged-welder"));
        }
    }

    private void OnTowerInteractUsing(Entity<CommunicationsTowerComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.State == CommunicationsTowerState.Broken)
            return;

        if (TryComp<RMCDeviceBreakerComponent>(args.Used, out var breaker) && ent.Comp.State != CommunicationsTowerState.Broken)
        {
            var doafter = new DoAfterArgs(EntityManager, args.User, breaker.DoAfterTime, new RMCDeviceBreakerDoAfterEvent(), args.Used, args.Target, args.Used)
            {
                BreakOnMove = true,
                RequireCanInteract = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            _doAfter.TryStartDoAfter(doafter);
            return;
        }

        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-comm-tower-wipe-radio")),
            new(Loc.GetString("rmc-comm-tower-add-radio")),
        };
        _dialog.OpenOptions(ent, args.User, "TC-3T телекоммуникационная вышка", options);
    }

    private void OnTowerDialogChosen(Entity<CommunicationsTowerComponent> ent, ref DialogChosenEvent args)
    {
        DoAfterEvent ev;
        var delay = TimeSpan.Zero;
        if (args.Index == 0)
        {
            ev = new CommunicationsTowerWipeDoAfterEvent();
        }
        else
        {
            ev = new CommunicationsTowerAddDoAfterEvent();
            delay = TimeSpan.FromSeconds(1);
        }

        var doAfter = new DoAfterArgs(EntityManager, args.Actor, delay, ev, ent)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTowerDialogWipeDoAfter(Entity<CommunicationsTowerComponent> ent, ref CommunicationsTowerWipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.State == CommunicationsTowerState.Broken)
            return;

        args.Handled = true;
        var msg = Loc.GetString("rmc-comm-tower-wipe-radio-done", ("tower", Name(ent)));
        _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
    }

    private void OnTowerDialogAddDoAfter(Entity<CommunicationsTowerComponent> ent, ref CommunicationsTowerAddDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.State == CommunicationsTowerState.Broken)
            return;

        var factions = new HashSet<EntProtoId<IFFFactionComponent>>();
        if (_gunIFF.TryGetFactions(args.User, factions))
        {
            foreach (var faction in factions)
            {
                if (_prototypes.TryIndex(faction, out var factionProto) &&
                    factionProto.TryGetComponent(out FactionFrequenciesComponent? frequencies, _compFactory))
                {
                    ent.Comp.Channels.UnionWith(frequencies.Channels);
                }
            }

            if (factions.Count > 0)
                Dirty(ent);
        }

        args.Handled = true;
        var msg = Loc.GetString("rmc-comm-tower-add-radio-done", ("tower", Name(ent)));
        _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
    }

    private void OnTowerInteractHand(Entity<CommunicationsTowerComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.State == CommunicationsTowerState.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-comm-tower-need-repair-to-work", ("tower", Name(ent))), ent, args.User, PopupType.MediumCaution);
            return;
        }

        if (!_rmcPower.IsPowered(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-comm-tower-out-of-power", ("tower", Name(ent))), ent, args.User, PopupType.MediumCaution);
            return;
        }

        var state = ent.Comp.State switch
        {
            CommunicationsTowerState.Off => CommunicationsTowerState.On,
            CommunicationsTowerState.On => CommunicationsTowerState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        _adminLog.Add(LogType.RMCCommunicationsTower, $"{ToPrettyString(args.User):user} turned {ToPrettyString(ent):tower} {state}.");

        ChangeState(ent, state);

        if (ent.Comp.State == CommunicationsTowerState.On)
            _intel.RestoreColonyCommunications();
    }

    private void OnTowerPowerChangedEvent(Entity<CommunicationsTowerComponent> ent, ref PowerChangedEvent args)
    {
        if (ent.Comp.State != CommunicationsTowerState.On)
            return;

        if (args.Powered)
        {
            _intel.RestoreColonyCommunications();
            return;
        }

        ChangeState(ent, CommunicationsTowerState.Off);
    }

    private void ChangeState(Entity<CommunicationsTowerComponent> tower, CommunicationsTowerState newState)
    {
        tower.Comp.State = newState;
        Dirty(tower);

        var ev = new CommunicationsTowerStateChangedEvent(tower);
        RaiseLocalEvent(tower, ev);
        UpdateAppearance(tower);
    }

    public bool CanTransmit(ProtoId<RadioChannelPrototype> channel)
    {
        var towers = EntityQueryEnumerator<CommunicationsTowerComponent>();
        while (towers.MoveNext(out var tower))
        {
            if (tower.State != CommunicationsTowerState.On ||
                !tower.Channels.Contains(channel))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void UpdateAppearance(Entity<CommunicationsTowerComponent> tower)
    {
        _appearance.SetData(tower, CommunicationsTowerLayers.Layer, tower.Comp.State);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        _spawners.Clear();
        var spawnersQuery = EntityQueryEnumerator<CommunicationsTowerSpawnerComponent>();
        while (spawnersQuery.MoveNext(out var uid, out var spawner))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            QueueDel(uid);
            _spawners.GetOrNew(spawner.Group).Add((uid, spawner));
        }

        foreach (var spawners in _spawners.Values)
        {
            if (spawners.Count == 0)
                continue;

            var spawner = _random.Pick(spawners);
            Spawn(spawner.Comp.Spawn, _transform.GetMoverCoordinates(spawner));
        }
    }
}
