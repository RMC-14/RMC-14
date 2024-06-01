using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Construction.Nest;

public sealed class XenoNestSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<Direction> _candidateNests = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoNestSurfaceComponent, ActivateInWorldEvent>(OnNestSurfaceActivate);
        SubscribeLocalEvent<XenoNestSurfaceComponent, DoAfterAttemptEvent<XenoNestDoAfterEvent>>(OnNestSurfaceDoAfterAttempt);
        SubscribeLocalEvent<XenoNestSurfaceComponent, XenoNestDoAfterEvent>(OnNestSurfaceDoAfter);

        SubscribeLocalEvent<XenoNestComponent, ComponentRemove>(OnNestRemove);
        SubscribeLocalEvent<XenoNestComponent, EntityTerminatingEvent>(OnNestTerminating);

        SubscribeLocalEvent<XenoNestedComponent, ComponentRemove>(OnNestedRemove);
        SubscribeLocalEvent<XenoNestedComponent, PreventCollideEvent>(OnNestedPreventCollide);
        SubscribeLocalEvent<XenoNestedComponent, PullAttemptEvent>(OnNestedPullAttempt);
        SubscribeLocalEvent<XenoNestedComponent, UpdateCanMoveEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, InteractionAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, UseAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, ThrowAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, PickupAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, AttackAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, ChangeDirectionAttemptEvent>(OnNestedCancel);
    }

    private void OnNestRemove(Entity<XenoNestComponent> ent, ref ComponentRemove args)
    {
        DetachNested(ent, ent.Comp.Nested);
    }

    private void OnNestTerminating(Entity<XenoNestComponent> ent, ref EntityTerminatingEvent args)
    {
        DetachNested(ent, ent.Comp.Nested);
    }

    private void OnNestedRemove(Entity<XenoNestedComponent> ent, ref ComponentRemove args)
    {
        DetachNested(null, ent);
    }

    private void OnNestSurfaceActivate(Entity<XenoNestSurfaceComponent> ent, ref ActivateInWorldEvent args)
    {
        if (TryComp(args.User, out PullerComponent? puller) &&
            puller.Pulling is { } pulling)
        {
            if (GetNestDirection(ent, pulling) is not { } direction ||
                !CanNestPopup(args.User, pulling, ent, direction))
            {
                return;
            }

            var ev = new XenoNestDoAfterEvent();
            // TODO CM14 before merge delay
            var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfter, ev, ent, pulling)
            {
                BreakOnMove = true,
                AttemptFrequency = AttemptFrequency.EveryTick
            };

            _doAfter.TryStartDoAfter(doAfter);

            // TODO CM14 make a method to do this
            var victimName = Identity.Name(pulling, EntityManager, args.User);
            _popup.PopupClient($"You pin {victimName} into the alien nest, preparing the securing resin.", args.User, args.User);

            foreach (var session in Filter.PvsExcept(args.User).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                var userName = Identity.Name(args.User, EntityManager, recipient);
                victimName = Identity.Name(pulling, EntityManager, recipient);

                if (recipient == pulling)
                {
                    _popup.PopupEntity($"{userName} pins you into the alien nest, preparing the securing resin.", args.User, recipient, PopupType.MediumCaution);
                }
                else
                {
                    _popup.PopupEntity($"{userName} pins {victimName} into the alien nest, preparing the securing resin.", args.User, recipient);
                }
            }
        }
    }

    private void OnNestSurfaceDoAfterAttempt(Entity<XenoNestSurfaceComponent> ent, ref DoAfterAttemptEvent<XenoNestDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target ||
            TerminatingOrDeleted(target) ||
            GetNestDirection(ent, target) is not { } direction ||
            !CanNestPopup(args.DoAfter.Args.User, target, ent, direction))
        {
            args.Cancel();
        }
    }

    private void OnNestSurfaceDoAfter(Entity<XenoNestSurfaceComponent> ent, ref XenoNestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } victim ||
            GetNestDirection(ent, victim) is not { } direction ||
            !CanNestPopup(args.User, victim, ent, direction) ||
            ent.Comp.Nests.ContainsKey(direction))
        {
            return;
        }

        args.Handled = true;

        if (TryComp(victim, out PullableComponent? pullable))
            _pulling.TryStopPull(victim, pullable);

        if (_net.IsClient)
            return;

        var nestCoordinates = ent.Owner.ToCoordinates();
        var offset = direction switch
        {
            Direction.South => new Vector2(0, -0.25f),
            Direction.East => new Vector2(0.5f, 0),
            Direction.North => new Vector2(0, 0.5f),
            Direction.West => new Vector2(-0.5f, 0),
            _ => Vector2.Zero
        };

        var nest = SpawnAttachedTo(ent.Comp.Nest, nestCoordinates);
        _transform.SetCoordinates(nest, nestCoordinates.Offset(offset));

        ent.Comp.Nests[direction] = nest;
        Dirty(ent);

        var nestComp = EnsureComp<XenoNestComponent>(nest);
        nestComp.Surface = ent;
        nestComp.Nested = victim;
        Dirty(nest, nestComp);

        var nestedComp = EnsureComp<XenoNestedComponent>(victim);
        nestedComp.Nest = nest;
        Dirty(victim, nestedComp);

        _transform.SetCoordinates(victim, nest.ToCoordinates());
        _transform.SetLocalRotation(victim, direction.ToAngle());

        _standingState.Stand(victim, force: true);

        // TODO CM14 make a method to do this
        var victimName = Identity.Name(victim, EntityManager, args.User);
        _popup.PopupClient($"You secrete a thick, vile resin, securing {victimName} into the alien nest!", args.User, args.User);

        foreach (var session in Filter.PvsExcept(args.User).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            var userName = Identity.Name(args.User, EntityManager, recipient);
            victimName = Identity.Name(victim, EntityManager, recipient);

            if (recipient == victim)
            {
                _popup.PopupEntity($"{userName} secretes a thick, vile resin, securing you into the alien nest!", args.User, recipient, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity($"{userName} secretes a thick, vile resin, securing {victimName} into the alien nest!", args.User, recipient);
            }
        }
    }

    private void OnNestedPreventCollide(Entity<XenoNestedComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNestedPullAttempt(Entity<XenoNestedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNestedCancel<T>(Entity<XenoNestedComponent> ent, ref T args) where T : CancellableEntityEventArgs
    {
        args.Cancel();
    }

    private Direction? GetNestDirection(EntityUid surface, EntityUid victim)
    {
        var victimCoords = _transform.GetMoverCoordinates(victim);
        var nestCoords = _transform.GetMoverCoordinates(surface);
        if (!nestCoords.TryDelta(EntityManager, _transform, victimCoords, out var delta))
            return null;

        return (new Angle(delta) + - MathHelper.PiOver2).GetCardinalDir();
    }

    private bool CanNestPopup(EntityUid user, EntityUid victim, EntityUid surface, Direction direction)
    {
        if (!HasComp<XenoNestableComponent>(victim))
        {
            var victimName = Identity.Name(victim, EntityManager, user);
            _popup.PopupClient($"{victimName} can't be put into a nest!", surface, user);
            return false;
        }

        if (!_standingState.IsDown(victim))
        {
            var victimName = Identity.Name(victim, EntityManager, user);
            _popup.PopupClient($"{victimName} is resisting, ground them!", victim, user, PopupType.MediumCaution);
            return false;
        }

        if (!TryComp(surface, out XenoNestSurfaceComponent? surfaceComp))
        {
            _popup.PopupClient("You can't create a nest there!", surface, user);
            return false;
        }

        if (surfaceComp.Nests.ContainsKey(direction))
        {
            _popup.PopupClient("There is already someone nested there!", surface, user);
            return false;
        }

        return true;
    }

    private void DetachNested(EntityUid? nest, EntityUid? nestedNullable)
    {
        if (_timing.ApplyingState)
            return;

        if (nestedNullable is not { } nested ||
            TerminatingOrDeleted(nested) ||
            !TryComp(nested, out TransformComponent? xform))
        {
            return;
        }

        if (TryComp(nested, out XenoNestedComponent? nestedComp))
        {
            nest ??= nestedComp.Nest;

            if (nestedComp.Detached)
                return;

            nestedComp.Detached = true;
            Dirty(nested, nestedComp);

            if (TryComp(nest, out XenoNestComponent? nestComp) &&
                TryComp(nestComp.Surface, out XenoNestSurfaceComponent? surfaceComp))
            {
                _candidateNests.Clear();
                foreach (var (dir, _) in surfaceComp.Nests)
                {
                    _candidateNests.Add(dir);
                }

                foreach (var dir in _candidateNests)
                {
                    surfaceComp.Nests.Remove(dir);
                }

                Dirty(nestComp.Surface.Value, surfaceComp);
            }
        }

        var position = xform.LocalPosition;
        _transform.SetLocalPosition(nested, position + xform.LocalRotation.ToWorldVec() / 2);
        _transform.AttachToGridOrMap(nested, xform);

        RemCompDeferred<XenoNestedComponent>(nested);
        QueueDel(nest);
    }
}
