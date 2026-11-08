using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dropship.Fabricator;

public sealed class DropshipFabricatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ARESCoreSystem _core = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerLoaderSystem _powerLoader = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private int _startingPoints;
    private TimeSpan _gainEvery;
    public ImmutableArray<EntProtoId<DropshipFabricatorPrintableComponent>> Printables { get; private set; }

    private static readonly EntProtoId<ARESLogTypeComponent> LogCat = "ARESTabDropshipLogs";

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<DropshipFabricatorComponent, MapInitEvent>(OnFabricatorMapInit);
        SubscribeLocalEvent<DropshipFabricatorComponent, DropshipFabricatoreRecycleDoafterEvent>(OnDropshipPartRecycled);

        Subs.BuiEvents<DropshipFabricatorComponent>(DropshipFabricatorUi.Key,
            subs =>
            {
                subs.Event<DropshipFabricatorPrintMsg>(OnPrintMsg);
                subs.Event<DropshipFabricatorCancelQueueMsg>(OnCancelQueueMsg);
            });

        Subs.CVar(_config, RMCCVars.RMCDropshipFabricatorStartingPoints, v => _startingPoints = v, true);
        Subs.CVar(_config, RMCCVars.RMCDropshipFabricatorGainEverySeconds, v => _gainEvery = TimeSpan.FromSeconds(v), true);

        ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPrototypes();
    }

    private void OnFabricatorMapInit(Entity<DropshipFabricatorComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer)
            ent.Comp.Account = EnsurePoints();
    }

    private void OnDropshipPartRecycled(Entity<DropshipFabricatorComponent> ent, ref DropshipFabricatoreRecycleDoafterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp(args.Used, out DropshipFabricatorPrintableComponent? printable) ||
            !TryComp(ent.Comp.Account, out DropshipFabricatorPointsComponent? points))
        {
            return;
        }

        args.Handled = true;

        var refund = printable.Cost;
        if (TryComp(args.Used, out DropshipAmmoComponent? ammo))
            refund = (int) (refund * (float) ammo.Rounds / ammo.MaxRounds);

        points.Points += (int) (refund * printable.RecycleMultiplier);
        Dirty(ent.Comp.Account.Value, points);
        SendUIStateAll(points.Points);
        Del(args.Used);

        _audio.PlayPvs(ent.Comp.RecycleSound, ent);
        _powerLoader.TrySyncHands(args.User);
    }

    private void OnPrintMsg(Entity<DropshipFabricatorComponent> ent, ref DropshipFabricatorPrintMsg args)
    {
        if (args.Id == default || !_prototypes.TryIndex(args.Id, out var proto))
            return;

        if (!proto.TryGetComponent(out DropshipFabricatorPrintableComponent? printable, _compFactory))
            return;

        var actor = args.Actor;
        if (!TryComp(ent.Comp.Account, out DropshipFabricatorPointsComponent? points))
            return;

        if (ent.Comp.Queue.Count >= ent.Comp.MaxQueue)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-fabricator-queue-full"), actor, actor, PopupType.SmallCaution);
            return;
        }

        if (printable.Cost > points.Points)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-fabricator-insufficient-points"), actor, actor, PopupType.SmallCaution);
            return;
        }

        points.Points -= printable.Cost;
        Dirty(ent.Comp.Account.Value, points);
        SendUIStateAll(points.Points);

        ent.Comp.Queue.Add(new DropshipFabricatorQueueEntry(proto.ID, printable.Cost));
        Dirty(ent);
        TryStartNextPrint(ent);

        _core.CreateARESLog(ent, LogCat, (string)$"{Name(args.Actor)} printed {proto.Name} for {printable.Cost} points at the dropship lathe");
    }

    private void OnCancelQueueMsg(Entity<DropshipFabricatorComponent> ent, ref DropshipFabricatorCancelQueueMsg args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        var entry = ent.Comp.Queue[args.Index];
        ent.Comp.Queue.RemoveAt(args.Index);

        if (TryComp(ent.Comp.Account, out DropshipFabricatorPointsComponent? points))
        {
            points.Points += entry.Cost;
            Dirty(ent.Comp.Account.Value, points);
            SendUIStateAll(points.Points);
        }

        Dirty(ent);
    }

    private bool TryStartNextPrint(Entity<DropshipFabricatorComponent> ent)
    {
        if (ent.Comp.Printing != null)
            return false;

        var changed = false;
        while (ent.Comp.Queue.Count > 0)
        {
            changed = true;
            var entry = ent.Comp.Queue[0];
            ent.Comp.Queue.RemoveAt(0);

            if (!_prototypes.TryIndex(entry.Id, out var proto) ||
                !proto.TryGetComponent(out DropshipFabricatorPrintableComponent? printable, _compFactory))
            {
                RefundQueuedCost(ent, entry.Cost);
                continue;
            }

            ent.Comp.Printing = entry.Id;
            ent.Comp.PrintAt = _timing.CurTime + printable.Delay;
            Dirty(ent);

            _appearance.SetData(ent, DropshipFabricatorVisuals.State, DropshipFabricatorState.Fabricating);
            return true;
        }

        if (changed)
            Dirty(ent);

        return false;
    }

    private void RefundQueuedCost(Entity<DropshipFabricatorComponent> ent, int cost)
    {
        if (!TryComp(ent.Comp.Account, out DropshipFabricatorPointsComponent? points))
            return;

        points.Points += cost;
        Dirty(ent.Comp.Account.Value, points);
        SendUIStateAll(points.Points);
    }

    private Entity<DropshipFabricatorPointsComponent> EnsurePoints()
    {
        var query = EntityQueryEnumerator<DropshipFabricatorPointsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            return (uid, comp);
        }

        var points = Spawn(null, MapCoordinates.Nullspace);
        var pointsComp = EnsureComp<DropshipFabricatorPointsComponent>(points);
        pointsComp.Points = _startingPoints;
        return (points, pointsComp);
    }

    private void ReloadPrototypes()
    {
        var printables = new List<EntityPrototype>();
        var prototypes = _prototypes.EnumeratePrototypes<EntityPrototype>();
        foreach (var prototype in prototypes)
        {
            if (prototype.HasComponent<DropshipFabricatorPrintableComponent>(_compFactory))
                printables.Add(prototype);
        }

        printables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        Printables = printables.Select(e => new EntProtoId<DropshipFabricatorPrintableComponent>(e.ID)).ToImmutableArray();
    }

    public void ChangeBudget(int amount)
    {
        var accountQuery = EntityQueryEnumerator<DropshipFabricatorPointsComponent>();
        while (accountQuery.MoveNext(out var uid, out var comp))
        {
            comp.Points += amount;
            Dirty(uid, comp);
            SendUIStateAll(comp.Points);
        }
    }

    private void SendUIStateAll(int points)
    {
        var fabricatorQuery = EntityQueryEnumerator<DropshipFabricatorComponent>();
        while (fabricatorQuery.MoveNext(out var fabricatorId, out var fabricator))
        {
            fabricator.Points = points;
            Dirty(fabricatorId, fabricator);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var allFabricatorQuery = EntityQueryEnumerator<DropshipFabricatorComponent, TransformComponent>();
        while (allFabricatorQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Printing == null)
            {
                TryStartNextPrint((uid, comp));
                continue;
            }

            if (time < comp.PrintAt)
                continue;

            var rotation = _transform.GetWorldRotation(xform);
            var coordinates = uid.ToCoordinates().Offset(comp.PrintOffset.Rotate(rotation));
            SpawnAtPosition(comp.Printing.Value, coordinates);

            comp.Printing = null;
            Dirty(uid, comp);

            if (!TryStartNextPrint((uid, comp)))
                _appearance.SetData(uid, DropshipFabricatorVisuals.State, DropshipFabricatorState.Idle);
        }

        var pointsQuery = EntityQueryEnumerator<DropshipFabricatorPointsComponent>();
        while (pointsQuery.MoveNext(out var pointsId, out var points))
        {
            if (time < points.NextPointsAt)
                continue;

            points.NextPointsAt = time + _gainEvery;
            points.Points++;
            Dirty(pointsId, points);

            SendUIStateAll(points.Points);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class DropshipFabricatoreRecycleDoafterEvent : SimpleDoAfterEvent
{
}
