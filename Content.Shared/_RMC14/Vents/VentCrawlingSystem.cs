using Content.Shared._RMC14.Map;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._RMC14.Vents;
public sealed class VentCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCMapSystem _rmcmap = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VentEntranceComponent, InteractHandEvent>(OnVentEntranceInteract);
        SubscribeLocalEvent<VentEntranceComponent, VentEnterDoafterEvent>(OnVentEnterDoafter);

        SubscribeLocalEvent<VentExitComponent, VentExitDoafterEvent>(OnVentExitDoafter);

        SubscribeLocalEvent<VentCrawlableComponent, MapInitEvent>(OnVentDuctInit);
        SubscribeLocalEvent<VentCrawlableComponent, MoveEvent>(OnVentDuctMove);
    }

    private void OnVentDuctInit(Entity<VentCrawlableComponent> vent, ref MapInitEvent args)
    {
        if (vent.Comp.TravelDirection == PipeDirection.Fourway)
            return;

        vent.Comp.TravelDirection = vent.Comp.TravelDirection.RotatePipeDirection(Transform(vent).LocalRotation);
    }

    private void OnVentDuctMove(Entity<VentCrawlableComponent> vent, ref MoveEvent args)
    {
        if (vent.Comp.TravelDirection == PipeDirection.Fourway)
            return;

        vent.Comp.TravelDirection = vent.Comp.TravelDirection.RotatePipeDirection(args.NewRotation);
    }

    private bool TryGetVent(EntityUid vent, [NotNullWhen(true)] out VentCrawlableComponent? ventComp, [NotNullWhen(true)] out ContainerSlot? container)
    {
        ventComp = null;
        container = null;

        if (!TryComp(vent, out ventComp) || !Transform(vent).Anchored)
            return false;

        container = _container.EnsureContainer<ContainerSlot>(vent, ventComp.ContainerId);

        return true;
    }

    private void OnVentEntranceInteract(Entity<VentEntranceComponent> vent, ref InteractHandEvent args)
    {
        if (args.Handled || (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded))
            return;

        if (!TryComp<VentCrawlerComponent>(args.User, out var crawl) || !TryGetVent(vent, out var comp, out var container))
            return;

        if (container.ContainedEntities.Count > comp.MaxEntities)
            return;

        args.Handled = true;

        var ev = new VentEnterDoafterEvent();

        var doafter = new DoAfterArgs(EntityManager, args.User, crawl.VentEnterDelay, ev, vent, vent)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doafter.TryStartDoAfter(doafter);
        _jitter.DoJitter(vent, crawl.VentEnterDelay, true);
    }

    private void OnVentEnterDoafter(Entity<VentEntranceComponent> vent, ref VentEnterDoafterEvent args)
    {
        if (args.Handled || args.Cancelled || (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded))
            return;

        if (!TryGetVent(vent, out var comp, out var container) ||
            (comp.MaxEntities != null && container.ContainedEntities.Count > comp.MaxEntities))
            return;

        args.Handled = true;

        _container.Insert(args.User, container);
        EnsureComp<VentCrawlingComponent>(args.User);
    }

    private void OnVentExitDoafter(Entity<VentExitComponent> vent, ref VentExitDoafterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryGetVent(vent, out var comp, out var container))
            return;

        _container.Remove(args.User, container, destination: vent.Owner.ToCoordinates().Offset(args.ExitDirection.ToVec()),
            localRotation: Transform(vent).LocalRotation);

        _transform.AttachToGridOrMap(args.User);
        RemCompDeferred<VentCrawlingComponent>(args.User);
    }

    public bool AreVentsConnectedInDirection(EntityUid ventOne, EntityUid ventTwo, PipeDirection direction)
    {
        if (!TryComp<VentCrawlableComponent>(ventOne, out var vent1) || !TryComp<VentCrawlableComponent>(ventTwo, out var vent2))
            return false;

        //Share the same layer Id, just in case someone wants to make stacked vents
        if (vent1.LayerId != vent2.LayerId)
            return false;

        //First vent doesn't contain direction at all
        if (!vent1.TravelDirection.HasDirection(direction))
            return false;

        //Second vent doesn't connect from this side
        if (!vent2.TravelDirection.HasDirection(direction.GetOpposite()))
            return false;

        //They connect from this direction
        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<VentCrawlingComponent, VentCrawlerComponent, InputMoverComponent>();

        while (query.MoveNext(out var uid, out var crawling, out var crawler, out var input))
        {
            if (_timing.CurTime < crawling.NextVentMoveTime)
                continue;

            var buttons = SharedMoverController.GetNormalizedMovement(input.HeldMoveButtons);

            var travellingDirection = PipeDirection.None;

            travellingDirection = buttons switch
            {
                MoveButtons.Up => PipeDirection.North,
                MoveButtons.Right => PipeDirection.East,
                MoveButtons.Down => PipeDirection.South,
                MoveButtons.Left => PipeDirection.West,
                _ => PipeDirection.None,
            };

            if (travellingDirection == PipeDirection.None)
                continue;

            if (!_container.TryGetContainingContainer(uid, out var container))
                continue;

            var queryAnchor = _rmcmap.GetAnchoredEntitiesEnumerator(container.Owner, travellingDirection.ToDirection());
            var travelled = false;

            while (queryAnchor.MoveNext(out var uid2))
            {
                if (!AreVentsConnectedInDirection(container.Owner, uid2, travellingDirection))
                    continue;

                if (!TryGetVent(uid2, out var comp, out var container2) ||
                    (comp.MaxEntities != null && container2.ContainedEntities.Count > comp.MaxEntities))
                    continue;

                _container.Insert(uid, container2);
                crawling.NextVentMoveTime = _timing.CurTime + crawler.VentCrawlDelay;
                Dirty(uid, crawling);
                travelled = true;
                break;
            }

            if (travelled || (TryComp<WeldableComponent>(uid, out var weld) && weld.IsWelded))
                continue;

            if (HasComp<VentExitComponent>(container.Owner) &&
                TryGetVent(container.Owner, out var ventComp, out var _) &&
                ventComp.TravelDirection.HasDirection(travellingDirection))
            {
                var ev = new VentExitDoafterEvent(travellingDirection.ToDirection());

                var doafter = new DoAfterArgs(EntityManager, uid, crawler.VentExitDelay, ev, container.Owner, container.Owner)
                {
                    BreakOnMove = true,
                    DuplicateCondition = DuplicateConditions.SameEvent,
                    CancelDuplicate = false,
                    BlockDuplicate = true
                };

                _doafter.TryStartDoAfter(doafter);
                _jitter.DoJitter(uid, crawler.VentExitDelay, true);
            }
        }
    }
}
