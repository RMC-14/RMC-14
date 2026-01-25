using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Marines;
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
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Pierce;

public sealed class XenoPierceSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly VanguardShieldSystem _vanguard = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<MarineComponent>> _pierceEnts = new();
    private readonly HashSet<EntityUid> _hitAlready = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPierceComponent, XenoPierceActionEvent>(OnXenoPierceAction);
    }

    private void OnXenoPierceAction(Entity<XenoPierceComponent> xeno, ref XenoPierceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        if (_transform.GetGrid(args.Target) is not { } gridId || !HasComp<MapGridComponent>(gridId))
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

        var tiles = _line.DrawLine(xenoCoords, target, TimeSpan.Zero, xeno.Comp.Range.Float(), out _);

        if (tiles.Count == 0)
            return;

        args.Handled = true;

        _hitAlready.Clear();
        var hits = 0;
        EntityUid? hitEnt = null;

        foreach (var tile in tiles)
        {
            _pierceEnts.Clear();
            var entTile = Spawn(xeno.Comp.Blocker, tile.Coordinates);

            // This won't get hostile xenos but this also doesn't currently hit non marines in can ability attack target
            _lookup.GetEntitiesInRange(entTile.ToCoordinates(), 0.5f, _pierceEnts);

            foreach (var ent in _pierceEnts)
            {
                if (!_interaction.InRangeUnobstructed(entTile, ent.Owner, xeno.Comp.Range.Float()))
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

                if (!_hitAlready.Add(ent))
                    continue;

                hits++;

                var change = _damage.TryChangeDamage(ent, _xeno.TryApplyXenoSlashDamageMultiplier(ent, xeno.Comp.Damage), origin: xeno, armorPiercing: xeno.Comp.AP, tool: xeno);

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
