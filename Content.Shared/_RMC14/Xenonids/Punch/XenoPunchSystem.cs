using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Punch;

public sealed class XenoPunchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPunchComponent, XenoPunchActionEvent>(OnXenoPunchAction);
    }

    private void OnXenoPunchAction(Entity<XenoPunchComponent> xeno, ref XenoPunchActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoPunchAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var targetId = args.Target;
        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var damage = _damageable.TryChangeDamage(targetId, _xeno.TryApplyXenoSlashDamageMultiplier(targetId, xeno.Comp.Damage), origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        var origin = _transform.GetMapCoordinates(xeno);

        _rmcMelee.DoLunge(xeno, targetId);
        _obstacleSlamming.MakeImmune(targetId);
        _size.KnockBack(targetId, origin, xeno.Comp.Range, xeno.Comp.Range, xeno.Comp.ThrowSpeed);

        if (!HasComp<XenoComponent>(targetId))
            _slow.TrySlowdown(targetId, xeno.Comp.SlowDuration);

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
    }
}
