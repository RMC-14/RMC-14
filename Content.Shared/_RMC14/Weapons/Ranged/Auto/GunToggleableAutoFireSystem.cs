using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.Auto;

public sealed class GunToggleableAutoFireSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<XenoComponent>> _targets = new();
    private readonly PolygonShape _shape = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GunToggleableAutoFireComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunToggleableAutoFireComponent, GunToggleableAutoFireActionEvent>(OnAutoFireAction);

        SubscribeLocalEvent<ActiveGunAutoFireComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ActiveGunAutoFireComponent, ItemUnwieldedEvent>(OnDoRemove);
        SubscribeLocalEvent<ActiveGunAutoFireComponent, DroppedEvent>(OnDoRemove);
        SubscribeLocalEvent<ActiveGunAutoFireComponent, GunUnpoweredEvent>(OnDoRemove);
    }

    private void OnGetItemActions(Entity<GunToggleableAutoFireComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnAutoFireAction(Entity<GunToggleableAutoFireComponent> ent, ref GunToggleableAutoFireActionEvent args)
    {
        args.Handled = true;

        if (EnsureComp<ActiveGunAutoFireComponent>(ent, out var comp))
        {
            RemCompDeferred<ActiveGunAutoFireComponent>(ent);
            AutoUpdated((ent, ent), false);
            return;
        }

        AutoUpdated((ent, ent), true);
    }

    private void OnRemove(Entity<ActiveGunAutoFireComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        AutoUpdated(ent.Owner, false);
    }

    private void OnDoRemove<T>(Entity<ActiveGunAutoFireComponent> ent, ref T args)
    {
        RemCompDeferred<ActiveGunAutoFireComponent>(ent);
        AutoUpdated(ent.Owner, false);
    }

    private void AutoUpdated(Entity<GunToggleableAutoFireComponent?> ent, bool active)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        _actions.SetToggled(ent.Comp.Action, active);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveGunAutoFireComponent, GunComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var active, out var gun, out var xform))
        {
            if (time < active.NextFire)
                continue;

            active.NextFire = time + active.FailCooldown;
            if (!_container.TryGetContainingContainer((uid, xform), out var container) ||
                !_hands.IsHolding(container.Owner, uid))
            {
                RemCompDeferred<ActiveGunAutoFireComponent>(uid);
                continue;
            }

            var (pos, rotation) = _transform.GetWorldPositionRotation(xform);
            var box = new Box2Rotated(Box2.CenteredAround(pos + rotation.ToWorldVec() * active.Range / 2, active.Range), rotation, pos + rotation.ToWorldVec() * active.Range / 2);
            var shapeTransform = Robust.Shared.Physics.Transform.Empty;

            _shape.Set(box);
            _targets.Clear();
            _entityLookup.GetEntitiesIntersecting(xform.MapID, _shape, shapeTransform, _targets);

            foreach (var target in _targets)
            {
                if (_mobState.IsDead(target))
                    continue;

                if (!_interaction.InRangeUnobstructed(container.Owner,
                        target.Owner,
                        10,
                        CollisionGroup.Impassable | CollisionGroup.BulletImpassable))
                {
                    continue;
                }

                _gun.AttemptShoot(container.Owner, uid, gun, target.Owner.ToCoordinates());
                active.NextFire = time + active.Cooldown;
                break;
            }
        }
    }
}
