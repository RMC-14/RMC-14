using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
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
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<EntityUid> pierceEnts = new();

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

        var target = args.Target;

        var xenoCoords = _transform.GetMoverCoordinates(xeno);

        if (!args.Target.TryDistance(EntityManager, xenoCoords, out var dis))
            return;

        if (dis > xeno.Comp.Range)
        {
            var direction = (args.Target.Position - xenoCoords.Position).Normalized();
            var newTile = direction * xeno.Comp.Range.Float();
            target = xenoCoords.WithPosition(xenoCoords.Position + newTile);
        }

        var tiles = _line.DrawLine(xenoCoords, target, TimeSpan.Zero, out _);

        if (tiles.Count == 0)
            return;

        args.Handled = true;

        var hits = 0;

        EntityUid? hitEnt = null;

        foreach (var tile in tiles)
        {
            pierceEnts.Clear();
            var entTile = Spawn(xeno.Comp.Blocker, tile.Coordinates);
            _lookup.GetEntitiesInRange(entTile, 0.5f, pierceEnts);

            foreach (var ent in pierceEnts)
            {

                if (!_interaction.InRangeUnobstructed(entTile, ent, xeno.Comp.Range.Float()))
                    continue;

                if (TryComp<DestroyOnXenoPierceScissorComponent>(ent, out var destroy))
                {
                    if (_net.IsServer)
                    {
                        SpawnAtPosition(destroy.SpawnPrototype, ent.ToCoordinates());
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
                    SpawnAttachedTo(xeno.Comp.AttackEffect, ent.ToCoordinates());

                if (hitEnt is null)
                    hitEnt = ent;

                if (xeno.Comp.MaxTargets != null && hits >= xeno.Comp.MaxTargets)
                    break;
            }
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
