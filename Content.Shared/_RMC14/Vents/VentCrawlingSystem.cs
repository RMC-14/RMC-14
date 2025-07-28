using Content.Shared._RMC14.Map;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Tools.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Maths;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Directions;
using Robust.Shared.Audio.Systems;
using Content.Shared.SubFloor;
using Content.Shared.Eye;

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
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    private bool _relativeMovement;
    public override void Initialize()
    {
        SubscribeLocalEvent<VentEntranceComponent, InteractHandEvent>(OnVentEntranceInteract);
        SubscribeLocalEvent<VentEntranceComponent, VentEnterDoafterEvent>(OnVentEnterDoafter);

        SubscribeLocalEvent<VentExitComponent, VentExitDoafterEvent>(OnVentExitDoafter);

        SubscribeLocalEvent<VentCrawlableComponent, MapInitEvent>(OnVentDuctInit);
        SubscribeLocalEvent<VentCrawlableComponent, MoveEvent>(OnVentDuctMove);

        SubscribeLocalEvent<VentCrawlingComponent, MoveInputEvent>(OnVentCrawlingInput);

        SubscribeLocalEvent<RMCTrayCrawlerComponent, GetVisMaskEvent>(OnTrayGetVis);

        Subs.CVar(_config, CCVars.RelativeMovement, v => _relativeMovement = v, true);
    }

    private void OnTrayGetVis(Entity<RMCTrayCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if(ent.Comp.Enabled)
            args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }
    private void OnVentDuctInit(Entity<VentCrawlableComponent> vent, ref MapInitEvent args)
    {
        if (vent.Comp.TravelDirection == PipeDirection.Fourway)
            return;

        vent.Comp.TravelDirection = vent.Comp.TravelDirection.RotatePipeDirection(Transform(vent).LocalRotation);
        Dirty(vent);
    }

    private void OnVentDuctMove(Entity<VentCrawlableComponent> vent, ref MoveEvent args)
    {
        if (vent.Comp.TravelDirection == PipeDirection.Fourway)
            return;

        vent.Comp.TravelDirection = vent.Comp.TravelDirection.RotatePipeDirection(args.NewRotation);
        Dirty(vent);
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

        if (container.ContainedEntities.Count > comp.MaxEntities || _container.IsEntityInContainer(args.User))
            return;

        args.Handled = true;

        var ev = new VentEnterDoafterEvent();

        var doafter = new DoAfterArgs(EntityManager, args.User, crawl.VentEnterDelay, ev, vent, vent)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doafter.TryStartDoAfter(doafter);
        _jitter.AddJitter(vent, 5, 8);
    }

    private void OnVentEnterDoafter(Entity<VentEntranceComponent> vent, ref VentEnterDoafterEvent args)
    {
        RemCompDeferred<JitteringComponent>(vent);
        if (args.Handled || args.Cancelled || (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded))
            return;

        if (!TryGetVent(vent, out var comp, out var container) ||
            (comp.MaxEntities != null && container.ContainedEntities.Count > comp.MaxEntities))
            return;

        args.Handled = true;

        _audio.PlayPredicted(vent.Comp.EnterSound, vent, args.User);

        _container.Insert(args.User, container);
        EnsureComp<VentCrawlingComponent>(args.User);
        if (TryComp<RMCTrayCrawlerComponent>(args.User, out var scanner))
        {
            scanner.Enabled = true;
            Dirty(args.User, scanner);
            _eye.RefreshVisibilityMask(args.User);
        }

    }

    private void OnVentExitDoafter(Entity<VentExitComponent> vent, ref VentExitDoafterEvent args)
    {
        RemCompDeferred<JitteringComponent>(vent);
        if (args.Handled || args.Cancelled)
            return;

        if (!TryGetVent(vent, out var comp, out var container))
            return;

        args.Handled = true;

        _container.Remove(args.User, container);
        _audio.PlayPredicted(vent.Comp.ExitSound, vent, args.User);

        _transform.AttachToGridOrMap(args.User);
        RemCompDeferred<VentCrawlingComponent>(args.User);
        if (TryComp<RMCTrayCrawlerComponent>(args.User, out var scanner))
        {
            scanner.Enabled = false;
            Dirty(args.User, scanner);
            _eye.RefreshVisibilityMask(args.User);
        }
    }

    private void OnVentCrawlingInput(Entity<VentCrawlingComponent> ent, ref MoveInputEvent args)
    {
        if (!TryComp<InputMoverComponent>(ent, out var input))
            return;

        var buttons = SharedMoverController.GetNormalizedMovement(input.HeldMoveButtons);

        var vectors = _mover.DirVecForButtons(buttons);

        if (vectors == System.Numerics.Vector2.Zero)
        {
            ent.Comp.TravelDirection = null;
            return;
        }

        var rotation = _mover.GetParentGridAngle(input);
        var wishDir = _relativeMovement ? rotation.RotateVec(vectors) : vectors;

        ent.Comp.TravelDirection = wishDir.GetDir().IsCardinal() ? wishDir.GetDir() : null;

        Dirty(ent);
    }

    public bool AreVentsConnectedInDirection(Entity<VentCrawlableComponent> sourceVent, Entity<VentCrawlableComponent> destinationVent, PipeDirection direction)
    {
        //Share the same layer Id, just in case someone wants to make stacked vents
        if (sourceVent.Comp.LayerId != destinationVent.Comp.LayerId)
            return false;

        //First vent doesn't contain direction at all
        if (!sourceVent.Comp.TravelDirection.HasDirection(direction))
            return false;

        //Second vent doesn't connect from this side
        if (!destinationVent.Comp.TravelDirection.HasDirection(direction.GetOpposite()))
            return false;

        //They connect from this direction
        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<VentCrawlingComponent, VentCrawlerComponent>();

        while (query.MoveNext(out var uid, out var crawling, out var crawler))
        {
            if (time < crawling.NextVentMoveTime)
                continue;

            if (crawling.TravelDirection == null)
                continue;

            if (!_container.TryGetContainingContainer(uid, out var container) || !TryComp<VentCrawlableComponent>(container.Owner, out var vent))
                continue;

            var queryAnchor = _rmcmap.GetAnchoredEntitiesEnumerator(container.Owner, crawling.TravelDirection.Value);
            var travelled = false;

            while (queryAnchor.MoveNext(out var uidDes))
            {
                if (!TryGetVent(uidDes, out var ventDes, out var containerDes))
                    continue;

                if (!AreVentsConnectedInDirection((container.Owner, vent), (uidDes, ventDes),
                    PipeDirectionHelpers.ToPipeDirection(crawling.TravelDirection.Value)))
                    continue;

                if ((ventDes.MaxEntities != null && containerDes.ContainedEntities.Count > ventDes.MaxEntities))
                    continue;

                _container.Insert(uid, containerDes);
                crawling.NextVentMoveTime = time + crawler.VentCrawlDelay;
                travelled = true;

                if (time >= crawling.NextVentCrawlSound)
                {
                    _audio.PlayPredicted(ventDes.TravelSound, uidDes, uid);
                    crawling.NextVentCrawlSound = time + crawler.VentCrawlSoundDelay;
                }

                Dirty(uid, crawling);
                break;
            }

            if (travelled || (TryComp<WeldableComponent>(uid, out var weld) && weld.IsWelded))
                continue;

            if (HasComp<VentExitComponent>(container.Owner) &&
                vent.TravelDirection.HasDirection(PipeDirectionHelpers.ToPipeDirection(crawling.TravelDirection.Value)))
            {
                var ev = new VentExitDoafterEvent();

                var doafter = new DoAfterArgs(EntityManager, uid, crawler.VentExitDelay, ev, container.Owner, container.Owner)
                {
                    BreakOnMove = true,
                    DuplicateCondition = DuplicateConditions.SameEvent,
                    CancelDuplicate = false,
                    BlockDuplicate = true
                };

                _doafter.TryStartDoAfter(doafter);
                _jitter.AddJitter(container.Owner, 5, 8);
            }
        }
    }
}
