using Content.Shared._RMC14.Map;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
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
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Audio.Systems;
using Content.Shared.Eye;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Coordinates;
using Content.Shared.Actions.Events;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared._RMC14.Storage.Containers;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._RMC14.Vents;
public abstract class SharedVentCrawlingSystem : EntitySystem
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    private bool _relativeMovement;
    public override void Initialize()
    {
        SubscribeLocalEvent<VentEntranceComponent, ExaminedEvent>(OnVentEntranceExamine);
        SubscribeLocalEvent<VentEntranceComponent, InteractHandEvent>(OnVentEntranceInteract);
        SubscribeLocalEvent<VentEntranceComponent, VentEnterDoafterEvent>(OnVentEnterDoafter);

        SubscribeLocalEvent<VentExitComponent, VentExitDoafterEvent>(OnVentExitDoafter);

        SubscribeLocalEvent<VentCrawlableComponent, MapInitEvent>(OnVentDuctInit);
        SubscribeLocalEvent<VentCrawlableComponent, MoveEvent>(OnVentDuctMove);
        SubscribeLocalEvent<VentCrawlableComponent, AnchorStateChangedEvent>(OnVentAnchorChanged);
        SubscribeLocalEvent<VentCrawlableComponent, RMCContainerDestructionEmptyEvent>(OnVentContainerDeletionEmpty);

        SubscribeLocalEvent<VentCrawlingComponent, MoveInputEvent>(OnVentCrawlingInput);
        SubscribeLocalEvent<VentCrawlingComponent, ComponentInit>(OnVentCrawlingStart);
        SubscribeLocalEvent<VentCrawlingComponent, ComponentRemove>(OnVentCrawlingEnd);
        SubscribeLocalEvent<VentCrawlingComponent, DropAttemptEvent>(OnVentCrawlingCancel);
        SubscribeLocalEvent<VentCrawlingComponent, PickupAttemptEvent>(OnVentCrawlingCancel);
        SubscribeLocalEvent<VentCrawlingComponent, UseAttemptEvent>(OnVentCrawlingCancel);

        SubscribeLocalEvent<RMCTrayCrawlerComponent, GetVisMaskEvent>(OnTrayGetVis);

        Subs.CVar(_config, CCVars.RelativeMovement, v => _relativeMovement = v, true);
    }

    private void OnVentEntranceExamine(Entity<VentEntranceComponent> vent, ref ExaminedEvent args)
    {
        if (!TryComp<VentCrawlerComponent>(args.Examiner, out var crawler))
            return;

        if (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded)
            return;

        args.PushMarkup(Loc.GetString(crawler.VentCrawlExamine));
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

    private void OnVentAnchorChanged(Entity<VentCrawlableComponent> vent, ref AnchorStateChangedEvent args)
    {
        EmptyVent(vent);
    }

    private void OnVentContainerDeletionEmpty(Entity<VentCrawlableComponent> vent, ref RMCContainerDestructionEmptyEvent args)
    {
        EmptyVent(vent);
    }

    private void EmptyVent(EntityUid vent)
    {
        if (!TryComp<VentCrawlableComponent>(vent, out var ventComp))
            return;

        var container = _container.EnsureContainer<Container>(vent, ventComp.ContainerId);

        var ents = _container.EmptyContainer(container, true);
        foreach (var en in ents)
        {
            RemoveVentCrawling(en);
        }
    }

    private bool TryGetVent(EntityUid vent, [NotNullWhen(true)] out VentCrawlableComponent? ventComp, [NotNullWhen(true)] out Container? container)
    {
        ventComp = null;
        container = null;

        if (!TryComp(vent, out ventComp) || !Transform(vent).Anchored)
            return false;

        //TODO fix multiple ents not fitting into the same pipe
        container = _container.EnsureContainer<Container>(vent, ventComp.ContainerId);

        return true;
    }

    private void OnVentEntranceInteract(Entity<VentEntranceComponent> vent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-welded"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryComp<VentCrawlerComponent>(args.User, out var crawl) || !TryGetVent(vent, out var comp, out var container))
            return;

        if ((comp.MaxEntities != null && container.ContainedEntities.Count > comp.MaxEntities))
        {
            _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-full"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (_container.IsEntityInContainer(args.User))
            return;

        var evn = new VentEnterAttemptEvent();

        RaiseLocalEvent(args.User, evn);

        if (evn.Cancelled)
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
        if (args.Handled || args.Cancelled)
            return;

        if (TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-welded"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryGetVent(vent, out var comp, out var container))
            return;

        if (comp.MaxEntities != null && container.ContainedEntities.Count > comp.MaxEntities)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-full"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var evn = new VentEnterAttemptEvent();

        RaiseLocalEvent(args.User, evn);

        if (evn.Cancelled)
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
            EnsureComp<VentSightComponent>(args.User);
        }

    }

    private void OnVentExitDoafter(Entity<VentExitComponent> vent, ref VentExitDoafterEvent args)
    {
        RemCompDeferred<JitteringComponent>(vent);
        if (args.Handled || args.Cancelled)
            return;

        if (!TryGetVent(vent, out var comp, out var container))
            return;

        if (_rmcmap.IsTileBlocked(vent.Owner.ToCoordinates()))
            return;

        args.Handled = true;

        _container.Remove(args.User, container);
        _audio.PlayPredicted(vent.Comp.ExitSound, vent, args.User);

        RemoveVentCrawling(args.User);

        _transform.AttachToGridOrMap(args.User);
    }

    private void RemoveVentCrawling(EntityUid ent)
    {
        RemCompDeferred<VentCrawlingComponent>(ent);
        if (TryComp<RMCTrayCrawlerComponent>(ent, out var scanner))
        {
            scanner.Enabled = false;
            Dirty(ent, scanner);
            _eye.RefreshVisibilityMask(ent);
            RemComp<VentSightComponent>(ent);
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

    private void OnVentCrawlingStart(Entity<VentCrawlingComponent> ent, ref ComponentInit args)
    {
        var actions = _actions.GetActions(ent);
        foreach (var action in actions)
        {
            _actions.SetEnabled(action.AsNullable(), false);
        }
    }

    private void OnVentCrawlingEnd(Entity<VentCrawlingComponent> ent, ref ComponentRemove args)
    {
        var actions = _actions.GetActions(ent);
        foreach (var action in actions)
        {
            _actions.SetEnabled(action.AsNullable(), true);
        }
    }

    private void OnVentCrawlingCancel<T>(Entity<VentCrawlingComponent> ent, ref T args) where T : CancellableEntityEventArgs
    {
        args.Cancel();
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

            if (!_mob.IsAlive(uid))
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
                {
                    _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-full"), uid, uid, PopupType.SmallCaution);
                    continue;
                }

                _container.Insert(uid, containerDes);
                crawling.NextVentMoveTime = time + crawler.VentCrawlDelay;
                travelled = true;

                if (time >= crawling.NextVentCrawlSound)
                {
                    _audio.PlayPredicted(ventDes.TravelSound, uidDes, uid);
                    crawling.NextVentCrawlSound = time + crawler.VentCrawlSoundDelay;
                    _popup.PopupPredictedCoordinates(Loc.GetString("rmc-vent-crawling-moving"), _transform.GetMoverCoordinates(uid), uid, PopupType.SmallCaution);
                }

                Dirty(uid, crawling);
                break;
            }

            if (travelled)
                continue;

            if (HasComp<VentExitComponent>(container.Owner) &&
                vent.TravelDirection.HasDirection(PipeDirectionHelpers.ToPipeDirection(crawling.TravelDirection.Value)))
            {
                if (TryComp<WeldableComponent>(container.Owner, out var weld) && weld.IsWelded)
                {
                    _popup.PopupPredicted(Loc.GetString("rmc-vent-crawling-welded"), uid, uid, PopupType.SmallCaution);
                    continue;
                }

                if (_rmcmap.IsTileBlocked(container.Owner.ToCoordinates()))
                    continue;

                var ev = new VentExitDoafterEvent();

                var doafter = new DoAfterArgs(EntityManager, uid, crawler.VentExitDelay, ev, container.Owner, container.Owner)
                {
                    BreakOnMove = true,
                    DuplicateCondition = DuplicateConditions.SameEvent,
                    CancelDuplicate = false,
                    BlockDuplicate = true,
                    RequireCanInteract = false
                };

                _doafter.TryStartDoAfter(doafter);
                _jitter.AddJitter(container.Owner, 5, 8);
            }
        }
    }
}
