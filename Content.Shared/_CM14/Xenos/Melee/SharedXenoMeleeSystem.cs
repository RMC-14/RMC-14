using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Melee;

public abstract class SharedXenoMeleeSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int AttackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

    protected Box2Rotated LastTailAttack;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoTailStabEvent>(OnXenoTailStab);
    }

    private void OnXenoTailStab(Entity<XenoComponent> xeno, ref XenoTailStabEvent args)
    {
        if (!_actionBlocker.CanAttack(xeno) ||
            !TryComp(xeno, out TransformComponent? transform))
        {
            return;
        }

        if (TryComp(xeno, out MeleeWeaponComponent? melee))
        {
            if (_timing.CurTime < melee.NextAttack)
                return;

            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
        }

        var userCoords = _transform.GetMapCoordinates(xeno, transform);
        if (userCoords.MapId == MapId.Nullspace)
            return;

        var targetCoords = args.Target.ToMap(EntityManager, _transform);
        if (userCoords.MapId != targetCoords.MapId)
            return;

        var tailRange = xeno.Comp.TailRange.Float();
        var box = new Box2(userCoords.Position.X - 0.10f, userCoords.Position.Y, userCoords.Position.X + 0.10f, userCoords.Position.Y + tailRange);

        var matrix = _transform.GetInvWorldMatrix(transform).Transform(targetCoords.Position);
        var rotation = _transform.GetWorldRotation(xeno).RotateVec(-matrix).ToWorldAngle();
        var boxRotated = new Box2Rotated(box, rotation, userCoords.Position);
        LastTailAttack = boxRotated;

        // ray on the left side of the box
        var leftRay = new CollisionRay(boxRotated.BottomLeft, (boxRotated.TopLeft - boxRotated.BottomLeft).Normalized(), AttackMask);

        // ray on the right side of the box
        var rightRay = new CollisionRay(boxRotated.BottomRight, (boxRotated.TopRight - boxRotated.BottomRight).Normalized(), AttackMask);

        // dont open allocations ahead
        // entity lookups dont work properly with Box2Rotated
        // so we do one ray cast on each side instead since its narrow enough
        // im sure you could calculate the ray bounds more efficiently
        // but have you seen these allocations either way
        var intersect = _physics.IntersectRayWithPredicate(transform.MapID, leftRay, tailRange,
            uid => uid == xeno.Owner || !HasComp<MobStateComponent>(uid), false);
        intersect = intersect.Concat(_physics.IntersectRayWithPredicate(transform.MapID, rightRay, tailRange,
            uid => uid == xeno.Owner || !HasComp<MobStateComponent>(uid), false));
        var results = intersect.Select(r => r.HitEntity).ToList();

        // TODO CM14 sounds
        // TODO CM14 lag compensation
        var damage = new DamageSpecifier(xeno.Comp.TailDamage);
        if (results.Count == 0)
        {
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), xeno, xeno, damage);
            RaiseLocalEvent(xeno, missEvent);
        }
        else
        {
            var hitEvent = new MeleeHitEvent(results, xeno, xeno, damage);
            RaiseLocalEvent(xeno, hitEvent);

            if (!hitEvent.Handled)
            {
                _interaction.DoContactInteraction(xeno, xeno);

                foreach (var hit in results)
                {
                    _interaction.DoContactInteraction(xeno, hit);
                }

                var filter = Filter.Pvs(transform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                foreach (var hit in results)
                {
                    var attackedEv = new AttackedEvent(xeno, xeno, args.Target);
                    RaiseLocalEvent(hit, attackedEv);

                    var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEv.BonusDamage, hitEvent.ModifiersList);
                    var change = _damageable.TryChangeDamage(hit, modifiedDamage, origin: xeno);

                    if (change?.GetTotal() > FixedPoint2.Zero)
                    {
                        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { hit }, filter);
                    }
                }
            }
        }

        var localPos = transform.LocalRotation.RotateVec(matrix);

        var length = localPos.Length();
        localPos *= tailRange / length;

        DoLunge((xeno, xeno, transform), localPos, "WeaponArcThrust");

        _audio.PlayPredicted(xeno.Comp.TailHitSound, xeno, xeno);

        var attackEv = new MeleeAttackEvent(xeno);
        RaiseLocalEvent(xeno, ref attackEv);

        args.Handled = true;
    }

    protected virtual void DoLunge(Entity<XenoComponent, TransformComponent> user, Vector2 localPos, EntProtoId animationId)
    {
    }
}
