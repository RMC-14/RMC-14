using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Rotate;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.TailJab;

public sealed class XenoTailJabSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _flash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly RMCSlowSystem _rmcSlow = default!;
    [Dependency] private readonly XenoRotateSystem _rotate = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailJabComponent, XenoTailJabActionEvent>(OnXenoImpaleAction);
    }

    private void OnXenoImpaleAction(Entity<XenoTailJabComponent> xeno, ref XenoTailJabActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        var target = args.Target;

        //TODO RMC14 targets chest
        var damage = new DamageSpecifier(xeno.Comp.Damage);
        var ev = new RMCGetTailStabBonusDamageEvent(new DamageSpecifier());
        RaiseLocalEvent(xeno, ref ev);
        damage += ev.Damage;

        var damageTaken = _damage.TryChangeDamage(target, _xeno.TryApplyXenoSlashDamageMultiplier(target, damage), origin: xeno, tool: xeno);
        if (damageTaken?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _flash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }

        _rmcMelee.DoLunge(xeno, target);
        _rmcSlow.TrySlowdown(target, xeno.Comp.SlowdownTime);
        _rmcObstacleSlamming.ApplyBonuses(target, xeno.Comp.WallSlamStunTime, xeno.Comp.WallSlamSlowdownTime);

        var origin = _transform.GetMoverCoordinates(xeno);
        var targetCoords = _transform.GetMoverCoordinates(target);
        var diff = (targetCoords.Position - origin.Position).Normalized() * xeno.Comp.ThrowRange;
        _throwing.TryThrow(target, diff, 5, animated: false); // throw slightly for wall slam behaviour

        var direction = _transform.GetWorldRotation(xeno).GetDir();
        var angle = direction.ToAngle() - Angle.FromDegrees(180);
        _rotate.RotateXeno(xeno, angle.GetDir());

        if (_net.IsClient)
            return;

        _audio.PlayPvs(xeno.Comp.Sound, xeno);
        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote, cooldown: xeno.Comp.EmoteCooldown);
        SpawnAttachedTo(xeno.Comp.AttackEffect, target.ToCoordinates());
    }
}
