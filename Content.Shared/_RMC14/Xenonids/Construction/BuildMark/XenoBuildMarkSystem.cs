using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Designer;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.BuildMark;

public sealed class XenoBuildMarkSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;

    private static readonly EntProtoId MarkId = "RMCXenoBuildMark";
    private static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(0.1);
    private const int MaxAreaSize = 15;
    private const int MaxAreaTiles = 50;
    private const float MovingVelocitySquared = 0.01f;

    private TimeSpan _nextCheck;
    private readonly List<MarkCandidate> _candidates = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<NewXenoEvolvedEvent>(OnNewXenoEvolved);
        SubscribeLocalEvent<XenoDevolvedEvent>(OnXenoDevolved);

        SubscribeLocalEvent<XenoBuildMarkComponent, EntityTerminatingEvent>(OnMarkTerminating);
        SubscribeLocalEvent<XenoBuildMarkerComponent, EntityTerminatingEvent>(OnMarkerTerminating);
    }

    private void OnNewXenoEvolved(ref NewXenoEvolvedEvent args)
    {
        TransferMarks(args.OldXeno, args.NewXeno);
    }

    private void OnXenoDevolved(ref XenoDevolvedEvent args)
    {
        TransferMarks(args.OldXeno, args.NewXeno);
    }

    private void OnMarkTerminating(Entity<XenoBuildMarkComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp(ent.Comp.Planter, out XenoBuildMarkerComponent? marker))
            marker.Marks.Remove(ent);
    }

    private void OnMarkerTerminating(Entity<XenoBuildMarkerComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        foreach (var mark in ent.Comp.Marks)
        {
            QueueDel(mark);
        }

        ent.Comp.Marks.Clear();
    }

    private void TransferMarks(EntityUid oldXeno, EntityUid newXeno)
    {
        if (_net.IsClient || !TryComp(oldXeno, out XenoBuildMarkerComponent? oldMarker))
            return;

        TryComp(newXeno, out XenoConstructionComponent? construction);
        var newMarker = EnsureComp<XenoBuildMarkerComponent>(newXeno);
        foreach (var markId in oldMarker.Marks)
        {
            if (!TryComp(markId, out XenoBuildMarkComponent? mark))
                continue;

            if (construction == null ||
                mark.Structure is not { } structure ||
                _xenoConstruction.GetBuildableVariant((newXeno, construction), structure) is not { } variant)
            {
                QueueDel(markId);
                continue;
            }

            mark.Planter = newXeno;
            mark.Structure = variant;
            mark.RequiresSupport = RequiresSupport(variant);
            mark.Queued = false;
            mark.PlanterInRange = false;
            Dirty(markId, mark);
            newMarker.Marks.Add(markId);
        }

        oldMarker.Marks.Clear();
    }

    public string GetMarkState(XenoBuildMarkComponent mark)
    {
        if (mark.Structure is { } structure && mark.States.TryGetValue(structure, out var state))
            return state;

        return mark.DefaultState;
    }

    public bool CanMark(EntProtoId structure)
    {
        return _prototype.TryIndex(structure, out var proto) &&
               !proto.HasComponent<DesignNodeComponent>();
    }

    public bool TryGetOwnMark(EntityUid xeno, EntityCoordinates coordinates, out Entity<XenoBuildMarkComponent> mark)
    {
        using var marks = _rmcMap.GetAnchoredEntitiesEnumerator<XenoBuildMarkComponent>(coordinates);
        while (marks.MoveNext(out var uid))
        {
            if (!TryComp(uid, out XenoBuildMarkComponent? comp) || comp.Planter != xeno)
                continue;

            mark = (uid, comp);
            return true;
        }

        mark = default;
        return false;
    }

    public bool IsSecreting(EntityUid xeno)
    {
        if (!TryComp(xeno, out DoAfterComponent? doAfter))
            return false;

        foreach (var active in doAfter.DoAfters.Values)
        {
            if (!active.Cancelled && !active.Completed && active.Args.Event is XenoSecreteStructureDoAfterEvent)
                return true;
        }

        return false;
    }

    private bool IsBusy(EntityUid xeno)
    {
        if (!TryComp(xeno, out DoAfterComponent? doAfter))
            return false;

        foreach (var active in doAfter.DoAfters.Values)
        {
            if (!active.Cancelled && !active.Completed)
                return true;
        }

        return false;
    }

    public void GetArea(Vector2i start, Vector2i end, bool line, List<Vector2i> tiles)
    {
        if (line)
        {
            GetLine(start, end, tiles);
            return;
        }

        var width = Math.Min(Math.Abs(end.X - start.X) + 1, MaxAreaSize);
        var height = Math.Min(Math.Abs(end.Y - start.Y) + 1, MaxAreaSize);
        while (width * height > MaxAreaTiles)
        {
            if (width >= height)
                width--;
            else
                height--;
        }

        var dirX = end.X >= start.X ? 1 : -1;
        var dirY = end.Y >= start.Y ? 1 : -1;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles.Add(new Vector2i(start.X + x * dirX, start.Y + y * dirY));
            }
        }
    }

    private static void GetLine(Vector2i start, Vector2i end, List<Vector2i> tiles)
    {
        var dx = Math.Abs(end.X - start.X);
        var dy = -Math.Abs(end.Y - start.Y);
        var stepX = end.X >= start.X ? 1 : -1;
        var stepY = end.Y >= start.Y ? 1 : -1;
        var error = dx + dy;
        var current = start;
        while (tiles.Count < MaxAreaSize)
        {
            tiles.Add(current);
            if (current == end)
                break;

            var doubled = error * 2;
            if (doubled >= dy)
            {
                error += dy;
                current.X += stepX;
            }

            if (doubled <= dx)
            {
                error += dx;
                current.Y += stepY;
            }
        }
    }

    public void PlaceMark(EntityUid xeno, EntityCoordinates coordinates, EntProtoId structure, bool busy)
    {
        var snapped = coordinates.SnapToGrid(EntityManager, _map);
        var markId = PredictedSpawnAtPosition(MarkId, snapped);
        var mark = EnsureComp<XenoBuildMarkComponent>(markId);

        mark.Planter = xeno;
        mark.Structure = structure;
        mark.RequiresSupport = RequiresSupport(structure);
        mark.Queued = HasComp<XenoAttachedOvipositorComponent>(xeno);
        Dirty(markId, mark);

        if (_net.IsClient)
            return;

        var marker = EnsureComp<XenoBuildMarkerComponent>(xeno);
        mark.PlanterInRange = !busy && InFillRange((xeno, marker), markId);
        marker.Marks.Add(markId);
        while (marker.Marks.Count > marker.MaxMarks)
        {
            QueueDel(marker.Marks[0]);
            marker.Marks.RemoveAt(0);
        }
    }

    public void RemoveMark(Entity<XenoBuildMarkComponent> mark, EntityUid xeno)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-build-mark-cleared"), mark, xeno);
        PredictedQueueDel(mark.Owner);
    }

    public void OnStructureBuilt(EntityUid xeno, EntityCoordinates coordinates)
    {
        if (_net.IsClient)
            return;

        using var marks = _rmcMap.GetAnchoredEntitiesEnumerator<XenoBuildMarkComponent>(coordinates);
        while (marks.MoveNext(out var uid))
        {
            QueueDel(uid);
        }

        if (!TryComp(xeno, out XenoBuildMarkerComponent? marker) ||
            !TryComp(xeno, out XenoConstructionComponent? construction))
        {
            return;
        }

        marker.ChainPending = true;
        TryContinue((xeno, marker, construction), true);
    }

    public void OnSecreteCancelled(EntityUid xeno, EntityCoordinates coordinates)
    {
        if (_net.IsClient)
            return;

        if (TryGetOwnMark(xeno, coordinates, out var mark))
            mark.Comp.PlanterInRange = false;
    }

    private float GetFillRange(Entity<XenoBuildMarkerComponent> xeno)
    {
        var ev = new XenoBuildMarkFillRangeEvent(xeno.Comp.FillRange);
        RaiseLocalEvent(xeno, ref ev);
        return ev.Range;
    }

    private bool InFillRange(Entity<XenoBuildMarkerComponent> xeno, EntityUid mark)
    {
        return _transform.InRange(_transform.GetMoverCoordinates(xeno), _transform.GetMoverCoordinates(mark), GetFillRange(xeno));
    }

    private bool TryBuildMark(Entity<XenoConstructionComponent> xeno, Entity<XenoBuildMarkComponent> mark, bool popup)
    {
        if (TerminatingOrDeleted(mark) || EntityManager.IsQueuedForDeletion(mark))
            return false;

        return mark.Comp.Structure is { } structure &&
               _xenoConstruction.TryStartSecrete(xeno, structure, _transform.GetMoverCoordinates(mark), popup);
    }

    private bool TryBuildCandidates(Entity<XenoConstructionComponent> xeno, bool queueOrder)
    {
        _candidates.Sort(queueOrder ? CompareQueued : CompareNearby);

        var popup = true;
        foreach (var candidate in _candidates)
        {
            if (TryBuildMark(xeno, (candidate.Uid, candidate.Mark), candidate.Entered && popup))
                return true;

            if (candidate.Entered)
                popup = false;
        }

        return false;
    }

    private static int CompareQueued(MarkCandidate a, MarkCandidate b)
    {
        var support = a.Mark.RequiresSupport.CompareTo(b.Mark.RequiresSupport);
        return support != 0 ? support : a.Order.CompareTo(b.Order);
    }

    private static int CompareNearby(MarkCandidate a, MarkCandidate b)
    {
        var support = a.Mark.RequiresSupport.CompareTo(b.Mark.RequiresSupport);
        if (support != 0)
            return support;

        var distance = b.Distance.CompareTo(a.Distance);
        return distance != 0 ? distance : a.Order.CompareTo(b.Order);
    }

    private bool RequiresSupport(EntProtoId structure)
    {
        return _prototype.TryIndex(structure, out var proto) &&
               proto.HasComponent<XenoConstructionRequiresSupportComponent>(_compFactory);
    }

    private void SweepDeadMarks(XenoBuildMarkerComponent marker)
    {
        foreach (var markId in marker.Marks)
        {
            if (EntityManager.IsQueuedForDeletion(markId))
                continue;

            var coordinates = _transform.GetMoverCoordinates(markId);
            if (!_rmcMap.HasAnchoredEntityEnumerator<XenoWeedsComponent>(coordinates, out _) ||
                _rmcMap.HasAnchoredEntityEnumerator<XenoConstructComponent>(coordinates, out _) ||
                _rmcMap.HasAnchoredEntityEnumerator<XenoEggComponent>(coordinates, out _) ||
                _rmcMap.HasAnchoredEntityEnumerator<XenoTunnelComponent>(coordinates, out _))
            {
                QueueDel(markId);
            }
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextCheck)
            return;

        _nextCheck = time + CheckEvery;

        var query = EntityQueryEnumerator<XenoBuildMarkerComponent, XenoConstructionComponent>();
        while (query.MoveNext(out var uid, out var marker, out var construction))
        {
            if (time >= marker.NextSweep)
            {
                marker.NextSweep = time + marker.SweepEvery;
                SweepDeadMarks(marker);
            }

            TryContinue((uid, marker, construction), time >= marker.NextQueueScan);
        }
    }

    private void TryContinue(Entity<XenoBuildMarkerComponent, XenoConstructionComponent> ent, bool scanQueue)
    {
        var (uid, marker, construction) = ent;
        if (marker.Marks.Count == 0)
        {
            marker.ChainPending = false;
            return;
        }

        if (IsBusy(uid) || IsMoving(uid))
            return;

        var xeno = new Entity<XenoConstructionComponent>(uid, construction);
        var onOvipositor = HasComp<XenoAttachedOvipositorComponent>(uid);
        if (onOvipositor && scanQueue)
        {
            marker.NextQueueScan = _timing.CurTime + marker.QueueScanEvery;
            _candidates.Clear();
            for (var i = 0; i < marker.Marks.Count; i++)
            {
                if (TryComp(marker.Marks[i], out XenoBuildMarkComponent? queued) && queued.Queued)
                    _candidates.Add(new MarkCandidate(marker.Marks[i], queued, i, 0, false));
            }

            if (TryBuildCandidates(xeno, true))
            {
                marker.ChainPending = false;
                return;
            }
        }

        var time = _timing.CurTime;
        var tile = _transform.GetMoverCoordinates(uid).SnapToGrid(EntityManager, _map);
        var retry = !tile.Equals(marker.LastTile) || time >= marker.NextRetry;
        marker.LastTile = tile;
        if (retry)
            marker.NextRetry = time + marker.RetryEvery;

        var chain = marker.ChainPending;
        marker.ChainPending = false;

        var xform = Transform(uid);
        var position = _transform.GetWorldPosition(xform);
        var fillRange = GetFillRange((uid, marker));
        _candidates.Clear();
        for (var i = 0; i < marker.Marks.Count; i++)
        {
            var markId = marker.Marks[i];
            if (!TryComp(markId, out XenoBuildMarkComponent? mark) ||
                onOvipositor && mark.Queued)
            {
                continue;
            }

            var markXform = Transform(markId);
            var distance = markXform.MapID == xform.MapID
                ? (_transform.GetWorldPosition(markXform) - position).Length()
                : float.MaxValue;

            var inRange = distance <= fillRange;
            var entered = inRange && !mark.PlanterInRange;
            mark.PlanterInRange = inRange;

            if (!inRange || !entered && !retry && !chain)
                continue;

            _candidates.Add(new MarkCandidate(markId, mark, i, distance, entered));
        }

        TryBuildCandidates(xeno, false);
    }

    private readonly record struct MarkCandidate(EntityUid Uid, XenoBuildMarkComponent Mark, int Order, float Distance, bool Entered);

    private bool IsMoving(EntityUid xeno)
    {
        return TryComp(xeno, out PhysicsComponent? physics) &&
               physics.LinearVelocity.LengthSquared() > MovingVelocitySquared;
    }
}
