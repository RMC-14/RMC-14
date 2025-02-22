using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.ScissorCut;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Pierce;

public sealed class XenoPierceSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly VanguardShieldSystem _vanguard = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPierceComponent, XenoPierceActionEvent>(OnXenoPierceAction);
    }

    private void OnXenoPierceAction(Entity<XenoPierceComponent> xeno, ref XenoPierceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(xeno, args.Action))
            return;

        if (_transform.GetGrid(args.Target) is not { } gridId ||
    !TryComp(gridId, out MapGridComponent? grid))
            return;

        args.Handled = true;

        var direction = (args.Target.Position - _transform.GetMoverCoordinates(xeno).Position).Normalized().ToAngle() - Angle.FromDegrees(90);

        var xenoCoord = _transform.GetMoverCoordinates(xeno);
        var area = Box2.CenteredAround(xenoCoord.Position, new(1, xeno.Comp.Range.Float())).Translated(new(0, (xeno.Comp.Range.Float() / 2) + 0.5f));
        var rot = new Box2Rotated(area, direction, xenoCoord.Position); // Correct the angle

        var hits = 0;

        EntityUid? hitEnt = null;

        foreach (var ent in _physics.GetCollidingEntities(Transform(xeno).MapID, rot))
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, ent.Owner, xeno.Comp.Range.Float() + 0.5f))
                continue;

            if (TryComp<DestroyOnXenoPierceScissorComponent>(ent, out var destroy))
            {
                if (_net.IsServer)
                {
                    SpawnAtPosition(destroy.SpawnPrototype, ent.Owner.ToCoordinates());
                    QueueDel(ent);
                }
                _audio.PlayPredicted(destroy.Sound, ent, xeno);
                continue;
            }

            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            hits++;

            var change = _damage.TryChangeDamage(ent, xeno.Comp.Damage, origin: xeno, armorPiercing: xeno.Comp.AP);

            if (change?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(ent, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { ent }, filter);
            }

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.AttackEffect, ent.Owner.ToCoordinates());

            if (hitEnt is null)
                hitEnt = ent;

            if (xeno.Comp.MaxTargets != null && hits >= xeno.Comp.MaxTargets)
                break;
        }

        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote, cooldown: xeno.Comp.EmoteCooldown);

        if (hits > 0 && hitEnt != null)
            _rmcMelee.DoLunge(xeno, hitEnt.Value);

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        if (hits >= xeno.Comp.RechargeTargetsRequired)
            _vanguard.RegenShield(xeno);
    }
}
