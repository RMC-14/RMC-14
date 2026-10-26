using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Flurry;

public sealed class XenoFlurrySystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;

    private HashSet<Entity<MobStateComponent>> _result = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFlurryComponent, XenoFlurryActionEvent>(OnXenoFlurryAction);
    }

    private void OnXenoFlurryAction(Entity<XenoFlurryComponent> xeno, ref XenoFlurryActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        if (_transform.GetGrid(args.Target) is not { } gridId || !TryComp(gridId, out MapGridComponent? grid))
            return;

        var targetPos = _transform.ToWorldPosition(args.Target);
        var xenoPos = _transform.GetWorldPosition(xeno);
        var direction = (targetPos - xenoPos).Normalized().ToAngle() - Angle.FromDegrees(90);

        var worldArea = Box2.CenteredAround(xenoPos, new(1, xeno.Comp.Range)).Translated(new(0, xeno.Comp.Range / 2 + 0.5f));
        var worldBox = new Box2Rotated(worldArea, direction, xenoPos);

        var map = Transform(xeno).MapID;

        if (_net.IsClient)
            return;

        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote, cooldown: xeno.Comp.EmoteDelay);

        _result.Clear();
        _entityLookup.GetEntitiesIntersecting(map, worldBox, _result, LookupFlags.Dynamic | LookupFlags.Static);

        if (_result.Count <= 0)
            return;

        //TODO RMC14 targets random limb
        var damage = new DamageSpecifier(xeno.Comp.Damage);
        var ev = new RMCGetTailStabBonusDamageEvent(new DamageSpecifier());
        RaiseLocalEvent(xeno, ref ev);
        damage += ev.Damage;

        var hits = 0;
        EntityUid? hitEnt = null;

        foreach (var victim in _result)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, victim))
                continue;

            if (!_interaction.InRangeUnobstructed(xeno.Owner, victim.Owner, xeno.Comp.Range + 0.5f))
                continue;

            if (hitEnt == null)
                hitEnt = victim;

            hits++;

            var change = _damage.TryChangeDamage(victim, _xeno.TryApplyXenoSlashDamageMultiplier(victim, damage), origin: xeno, tool: xeno);

            if (change?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(victim, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { victim }, filter);
            }

            SpawnAttachedTo(xeno.Comp.AttackEffect, victim.Owner.ToCoordinates());
            _audio.PlayEntity(xeno.Comp.SlashSound, xeno, victim);

            SpawnAttachedTo(xeno.Comp.HealEffect, xeno.Owner.ToCoordinates());
            _xenoHeal.CreateHealStacks(xeno, xeno.Comp.HealAmount, xeno.Comp.HealDelay, xeno.Comp.HealCharges, xeno.Comp.HealDelay);

            if (xeno.Comp.MaxTargets != null && hits >= xeno.Comp.MaxTargets)
                break;
        }

        if (hitEnt != null)
            _rmcMelee.DoLunge(xeno, hitEnt.Value);

        foreach (var tile in _map.GetTilesIntersecting(gridId, grid, worldBox))
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, _turf.GetTileCenter(tile), xeno.Comp.Range + 0.5f))
                continue;

            SpawnAtPosition(xeno.Comp.TelegraphEffect, _turf.GetTileCenter(tile));
        }
    }
}
