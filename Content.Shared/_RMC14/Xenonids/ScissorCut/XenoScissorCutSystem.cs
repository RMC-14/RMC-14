using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Empower;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.ScissorCut;

public sealed class XenoScissorCutSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoScissorCutComponent, XenoScissorCutActionEvent>(OnXenoScissorCutAction);
    }

    private void OnXenoScissorCutAction(Entity<XenoScissorCutComponent> xeno, ref XenoScissorCutActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        var slows = HasComp<XenoSuperEmpoweredComponent>(xeno);
        args.Handled = true;

        if (_transform.GetGrid(args.Target) is not { } gridId ||
    !TryComp(gridId, out MapGridComponent? grid))
            return;

        var direction = (args.Target.Position - _transform.GetMoverCoordinates(xeno).Position).Normalized().ToAngle() - Angle.FromDegrees(90);

        var xenoCoord = _transform.GetMoverCoordinates(xeno);
        var area = Box2.CenteredAround(xenoCoord.Position, new(1, xeno.Comp.Range)).Translated(new(0, (xeno.Comp.Range / 2) + 0.5f));
        var rot = new Box2Rotated(area, direction, xenoCoord.Position); // Correct the angle

        List<EntityUid> destructibles = new();
        List<EntityUid> mobs = new();

        if (_net.IsClient)
            return;

        foreach (var ent in _physics.GetCollidingEntities(Transform(xeno).MapID, rot))
        {
            if (HasComp<DamageOnXenoScissorsComponent>(ent) || HasComp<DestroyOnXenoPierceScissorComponent>(ent))
            {
                destructibles.Add(ent);
                continue;
            }

            if (!_xeno.CanAbilityAttackTarget(xeno, ent, false, true))
                continue;
            mobs.Add(ent);
        }

        var selfCoords = _transform.GetMoverCoordinates(xeno);

        //Have to sort so multi fence destruction happens in order
        destructibles = destructibles.OrderBy(a =>
        (selfCoords.TryDistance(EntityManager, a.ToCoordinates(), out var distance) ? distance : 10)).ToList();

        foreach (var des in destructibles)
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, des, xeno.Comp.Range + 0.5f))
                continue;

            if (TryComp<DamageOnXenoScissorsComponent>(des, out var destruct))
            {
                var dam = _damage.TryChangeDamage(des, destruct.Damage, origin: xeno, tool: xeno);

                if (dam?.GetTotal() > FixedPoint2.Zero)
                {
                    var filter = Filter.Pvs(des, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                    _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { des }, filter);
                }

                continue;
            }

            if (!TryComp<DestroyOnXenoPierceScissorComponent>(des, out var destoy))
                continue;


            SpawnAtPosition(destoy.SpawnPrototype, des.ToCoordinates());
            QueueDel(des);

            _audio.PlayEntity(destoy.Sound, des, xeno);
            continue;
        }

        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote);

        //Now mobs
        EntityUid? hitEnt = null;
        foreach (var victim in mobs)
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, victim, xeno.Comp.Range + 0.5f))
                continue;

            if (hitEnt == null)
                hitEnt = victim;

            var change = _damage.TryChangeDamage(victim, xeno.Comp.Damage, origin: xeno, tool: xeno);

            if (change?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(victim, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { victim }, filter);
            }

            SpawnAttachedTo(xeno.Comp.AttackEffect, victim.ToCoordinates());
            _audio.PlayEntity(xeno.Comp.SlashSound, xeno, victim);

            if (slows)
                _slow.TrySuperSlowdown(victim, xeno.Comp.SuperSlowDuration, ignoreDurationModifier: true);
        }

        if (hitEnt != null)
            _rmcMelee.DoLunge(xeno, hitEnt.Value);

        var bounds = rot.CalcBoundingBox();

        foreach (var tile in _map.GetTilesIntersecting(gridId, grid, rot))
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, _turf.GetTileCenter(tile), xeno.Comp.Range + 0.5f))
                continue;

            var spawn = xeno.Comp.TelegraphEffect;

            if (!bounds.Encloses(Box2.CenteredAround(_turf.GetTileCenter(tile).Position, Vector2.One)))
                spawn = xeno.Comp.TelegraphEffectEdge;

            SpawnAtPosition(spawn, _turf.GetTileCenter(tile));
        }
    }
}
