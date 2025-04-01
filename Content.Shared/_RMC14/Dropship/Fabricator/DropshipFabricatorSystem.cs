using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dropship.Fabricator;

public sealed class DropshipFabricatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private int _startingPoints;
    private TimeSpan _gainEvery;

    public ImmutableArray<EntProtoId<DropshipFabricatorPrintableComponent>> Printables { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<DropshipFabricatorComponent, MapInitEvent>(OnFabricatorMapInit);

        Subs.BuiEvents<DropshipFabricatorComponent>(DropshipFabricatorUi.Key,
            subs =>
            {
                subs.Event<DropshipFabricatorPrintMsg>(OnPrintMsg);
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

    private void OnPrintMsg(Entity<DropshipFabricatorComponent> ent, ref DropshipFabricatorPrintMsg args)
    {
        if (args.Id == default || !_prototypes.TryIndex(args.Id, out var proto))
            return;

        if (!proto.TryGetComponent(out DropshipFabricatorPrintableComponent? printable, _compFactory))
            return;

        var actor = args.Actor;
        if (ent.Comp.Printing != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-fabricator-busy"), actor, actor, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(ent.Comp.Account, out DropshipFabricatorPointsComponent? points))
            return;

        if (printable.Cost > points.Points)
            return;

        points.Points -= printable.Cost;
        Dirty(ent.Comp.Account.Value, points);

        ent.Comp.Points = points.Points;
        ent.Comp.Printing = proto.ID;
        ent.Comp.PrintAt = _timing.CurTime + printable.Delay;
        Dirty(ent);

        _appearance.SetData(ent, DropshipFabricatorVisuals.State, DropshipFabricatorState.Fabricating);
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
            if (time < comp.PrintAt)
                continue;

            if (comp.Printing == null)
                continue;

            var rotation = _transform.GetWorldRotation(xform);
            var coordinates = uid.ToCoordinates().Offset(comp.PrintOffset.Rotate(rotation));
            SpawnAtPosition(comp.Printing.Value, coordinates);

            comp.Printing = null;
            Dirty(uid, comp);

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
