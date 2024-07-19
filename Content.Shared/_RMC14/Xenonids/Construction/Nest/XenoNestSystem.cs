using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

public sealed class XenoNestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;

    private EntityQuery<XenoWeedableComponent> _xenoWeedable;

    public override void Initialize()
    {
        base.Initialize();

        _xenoWeedable = GetEntityQuery<XenoWeedableComponent>();

        SubscribeLocalEvent<XenoComponent, GetUsedEntityEvent>(OnXenoGetUsedEntity);

        SubscribeLocalEvent<XenoNestSurfaceComponent, InteractHandEvent>(OnSurfaceInteractHand);
        SubscribeLocalEvent<XenoNestableComponent, ActivateInWorldEvent>(OnNestedActivateInWorld);
        SubscribeLocalEvent<XenoNestSurfaceComponent, DoAfterAttemptEvent<XenoNestDoAfterEvent>>(OnSurfaceDoAfterAttempt);
        SubscribeLocalEvent<XenoNestedComponent, XenoUnNestDoAfterEvent>(OnUnNestNestableDoAfter);
        SubscribeLocalEvent<XenoNestSurfaceComponent, XenoNestDoAfterEvent>(OnNestSurfaceDoAfter);
        SubscribeLocalEvent<XenoNestSurfaceComponent, CanDropTargetEvent>(OnSurfaceCanDropTarget);
        SubscribeLocalEvent<XenoNestSurfaceComponent, DragDropTargetEvent>(OnSurfaceDragDropTarget);
        SubscribeLocalEvent<XenoNestSurfaceComponent, EntityTerminatingEvent>(OnSurfaceTerminating);

        SubscribeLocalEvent<XenoNestComponent, ComponentRemove>(OnNestRemove);
        SubscribeLocalEvent<XenoNestComponent, EntityTerminatingEvent>(OnNestTerminating);

        SubscribeLocalEvent<XenoNestableComponent, BeforeRangedInteractEvent>(OnNestableBeforeRangedInteract);

        SubscribeLocalEvent<XenoNestedComponent, ComponentRemove>(OnNestedRemove);
        SubscribeLocalEvent<XenoNestedComponent, PreventCollideEvent>(OnNestedPreventCollide);
        SubscribeLocalEvent<XenoNestedComponent, PullAttemptEvent>(OnNestedPullAttempt);
        SubscribeLocalEvent<XenoNestedComponent, InteractionAttemptEvent>(OnNestedInteractionAttempt);
        SubscribeLocalEvent<XenoNestedComponent, UpdateCanMoveEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, UseAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, ThrowAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, PickupAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, AttackAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, ChangeDirectionAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, DownAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, IsEquippingAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, IsUnequippingAttemptEvent>(OnNestedCancel);
        SubscribeLocalEvent<XenoNestedComponent, GetInfectedIncubationMultiplierEvent>(OnInNestGetInfectedIncubationMultiplier);
        SubscribeLocalEvent<XenoNestedComponent, ComponentStartup>(OnNestedAdd);
    }

    private void OnXenoGetUsedEntity(Entity<XenoComponent> ent, ref GetUsedEntityEvent args)
    {
        if (args.Handled ||
            CompOrNull<PullerComponent>(ent)?.Pulling is not { } pulling ||
            !HasComp<XenoNestableComponent>(pulling) ||
            HasComp<XenoNestedComponent>(pulling))
        {
            return;
        }

        args.Used = pulling;
    }

    private void OnSurfaceInteractHand(Entity<XenoNestSurfaceComponent> ent, ref InteractHandEvent args)
    {
        if (CompOrNull<PullerComponent>(args.User)?.Pulling is not { } pulling)
            return;

        args.Handled = true;
        TryStartNesting(args.User, ent, pulling);
    }

    private void OnNestedActivateInWorld(Entity<XenoNestableComponent> target, ref ActivateInWorldEvent args)
    {
        TryStartUnNesting(args.User, target.Owner);
    }

    private void OnNestRemove(Entity<XenoNestComponent> ent, ref ComponentRemove args)
    {
        DetachNested(ent, ent.Comp.Nested);
    }

    private void OnNestTerminating(Entity<XenoNestComponent> ent, ref EntityTerminatingEvent args)
    {
        DetachNested(ent, ent.Comp.Nested);
    }

    private void OnNestableBeforeRangedInteract(Entity<XenoNestableComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (!args.CanReach || !TryComp(args.Target, out XenoNestSurfaceComponent? surface))
            return;

        args.Handled = true;
        TryStartNesting(args.User, (args.Target.Value, surface), args.Used);
    }

	private void OnNestedAdd(Entity<XenoNestedComponent> ent, ref ComponentStartup args)
	{
		_parasite.RefreshIncubationMultipliers(ent.Owner);
	}

	private void OnNestedRemove(Entity<XenoNestedComponent> ent, ref ComponentRemove args)
    {
        DetachNested(null, ent);
        _actionBlocker.UpdateCanMove(ent);

        _parasite.RefreshIncubationMultipliers(ent.Owner);

        // TODO RMC14
        if (HasComp<KnockedDownComponent>(ent) || _mobState.IsIncapacitated(ent))
            _standing.Down(ent);
    }

    private void OnSurfaceDoAfterAttempt(Entity<XenoNestSurfaceComponent> ent, ref DoAfterAttemptEvent<XenoNestDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target ||
            TerminatingOrDeleted(target) ||
            !CanNestPopup(args.DoAfter.Args.User, target, ent, out _))
        {
            args.Cancel();
        }
    }

    private void OnNestSurfaceDoAfter(Entity<XenoNestSurfaceComponent> ent, ref XenoNestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } victim ||
            !CanNestPopup(args.User, victim, ent, out var direction))
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

        ent.Comp.Nests[direction.Value] = nest;
        Dirty(ent);

        var nestComp = EnsureComp<XenoNestComponent>(nest);
        nestComp.Surface = ent;
        nestComp.Nested = victim;
        Dirty(nest, nestComp);

        var nestedComp = EnsureComp<XenoNestedComponent>(victim);
        nestedComp.Nest = nest;
        Dirty(victim, nestedComp);

        _transform.SetCoordinates(victim, nest.ToCoordinates());
        _transform.SetLocalRotation(victim, direction.Value.ToAngle());

        _standing.Stand(victim, force: true);

        // TODO RMC14 make a method to do this
        _popup.PopupClient(Loc.GetString("cm-xeno-nest-securing-self", ("target", victim)), args.User, args.User);

        foreach (var session in Filter.PvsExcept(args.User).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            if (recipient == victim)
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-securing-target", ("user", args.User)), args.User, recipient, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-securing-observer", ("user", args.User), ("target", victim)), args.User, recipient);
            }
        }
    }

    private void OnUnNestNestableDoAfter(Entity<XenoNestedComponent> ent, ref XenoUnNestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;
        var target = ent.Owner;
        if (_net.IsClient)
            return;
        DetachNested(null, target);
    }

    #region DragDrop

    private void OnSurfaceCanDropTarget(Entity<XenoNestSurfaceComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanBeNested(args.User, args.Dragged, (ent, ent.Comp), silent: true);
        args.Handled = true;
    }

    private void OnSurfaceDragDropTarget(Entity<XenoNestSurfaceComponent> ent, ref DragDropTargetEvent args)
    {
        args.Handled = true;
        TryStartNesting(args.User, ent, args.Dragged);
    }

    private void OnSurfaceTerminating(Entity<XenoNestSurfaceComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TerminatingOrDeleted(ent.Comp.Weedable) &&
            _xenoWeedable.TryComp(ent.Comp.Weedable, out var weedable) &&
            weedable.Entity == ent)
        {
            weedable.Entity = null;
            Dirty(ent.Comp.Weedable.Value, weedable);
        }
    }

    #endregion

    private void OnNestedPreventCollide(Entity<XenoNestedComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNestedPullAttempt(Entity<XenoNestedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNestedInteractionAttempt(Entity<XenoNestedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNestedCancel<T>(Entity<XenoNestedComponent> ent, ref T args) where T : CancellableEntityEventArgs
    {
        args.Cancel();
    }

    private void OnInNestGetInfectedIncubationMultiplier(Entity<XenoNestedComponent> ent, ref GetInfectedIncubationMultiplierEvent args)
    {
        if (ent.Comp.Running)
            args.Multiply(ent.Comp.IncubationMultiplier);
	}

    private void TryStartNesting(EntityUid user, Entity<XenoNestSurfaceComponent> surface, EntityUid victim)
    {
        if (!HasComp<XenoComponent>(user) ||
            !CanNestPopup(user, victim, surface, out _))
        {
            return;
        }

        var ev = new XenoNestDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, surface.Comp.DoAfter, ev, surface, victim)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfter);

        // TODO RMC14 make a method to do this
        _popup.PopupClient(Loc.GetString("cm-xeno-nest-pin-self", ("target", victim)), user, user);

        foreach (var session in Filter.PvsExcept(user).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            if (recipient == victim)
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-pin-target", ("user", user)), user, recipient, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-pin-observer", ("user", user), ("target", victim)), user, recipient);
            }
        }
    }

    private void TryStartUnNesting(EntityUid user, EntityUid target)
    {
        if (!CanBeUnNested(user, target))
            return;

        foreach (var session in Filter.PvsExcept(user).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;
            if (recipient == target)
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-unsecuring-target", ("user", user)), user, recipient, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-nest-unsecuring-observer", ("user", user), ("target", target)), user, recipient);
            }
        }

        if (_mobState.IsDead(target) || _mobState.IsCritical(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-nest-unsecuring-inactive-self", ("target", target)), user, user);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-nest-unsecuring-active-self", ("target", target)), user, user);
        }
        var ev = new XenoUnNestDoAfterEvent();
        var doafterTime = TimeSpan.FromSeconds(8);
        var doAfter = new DoAfterArgs(EntityManager, user, doafterTime, ev, target)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private bool CanBeNested(EntityUid user,
        EntityUid victim,
        Entity<XenoNestSurfaceComponent?> surface,
        bool silent = false)
    {
        if (!HasComp<XenoNestableComponent>(victim))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-nest-failed", ("target", victim)), surface, user);

            return false;
        }

        if (!Resolve(surface, ref surface.Comp))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-nest-failed-cant-there"), surface, user);

            return false;
        }

        return true;
    }

    private bool CanBeUnNested(EntityUid user, EntityUid target) // potentially add surface to allow targeting surface for action , Entity<XenoNestSurfaceComponent?> surface
    {
        if (HasComp<XenoNestedComponent>(target) && HasComp<XenoComponent>(user))
            return true;
        return false;
    }

    private bool CanNestPopup(EntityUid user,
        EntityUid victim,
        Entity<XenoNestSurfaceComponent> surface,
        [NotNullWhen(true)] out Direction? direction,
        bool silent = false)
    {
        direction = null;

        if (!CanBeNested(user, victim, (surface, surface.Comp), silent))
            return false;

        if (!_standing.IsDown(victim))
        {
            if (!silent)
                _popup.PopupClient(Loc.GetString("cm-xeno-nest-failed-target-resisting", ("target", victim)), victim, user, PopupType.MediumCaution);

            return false;
        }

        // Check cardinal face or two cardinals when diagonal
        var userCoords = _transform.GetMoverCoordinates(user);
        var nestCoords = _transform.GetMoverCoordinates(surface);
        if (!userCoords.TryDelta(EntityManager, _transform, nestCoords, out var delta))
            return false;

        var attemptedDirection = delta.GetDir();
        var priorityDir = Angle.FromWorldVec(delta).GetCardinalDir();
        if (attemptedDirection is Direction.Invalid)
            return false;

        var directions = new List<Direction>();

        // Add priority first
        directions.Add(priorityDir);

        var flags = attemptedDirection.AsFlag();
        // If directions doesn't contain the direction and flag is set, add it.
        if (!directions.Contains(Direction.South) && flags.HasFlag(DirectionFlag.South))
            directions.Add(Direction.South);
        if (!directions.Contains(Direction.East) && flags.HasFlag(DirectionFlag.East))
            directions.Add(Direction.East);
        if (!directions.Contains(Direction.North) && flags.HasFlag(DirectionFlag.North))
            directions.Add(Direction.North);
        if (!directions.Contains(Direction.West) && flags.HasFlag(DirectionFlag.West))
            directions.Add(Direction.West);

        string? response = null;
        foreach (var dir in directions)
        {
            if (nestCoords.Offset(dir.ToVec()).GetTileRef(EntityManager, _map) is not { } tile ||
                   _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
            {
                response ??= Loc.GetString("cm-xeno-nest-failed-cant-there");
                continue;
            }

            if (surface.Comp.Nests.ContainsKey(dir))
            {
                response ??= Loc.GetString("cm-xeno-nest-failed-cant-already-there");
                continue;
            }

            response = null;
            direction = dir;
            break;
        }

        if (direction == null)
        {
            if (!silent)
                _popup.PopupClient(response, surface, user);

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
                foreach (var (dir, nestUid) in surfaceComp.Nests)
                {
                    if (nestUid != nest)
                        continue;

                    surfaceComp.Nests.Remove(dir);
                    break;
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
