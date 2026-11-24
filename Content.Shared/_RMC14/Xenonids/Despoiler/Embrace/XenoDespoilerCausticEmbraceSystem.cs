using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Despoiler.Catalyze;
using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Content.Shared._RMC14.Xenonids.Despoiler.Stab;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Embrace;

public sealed class XenoDespoilerCausticEmbraceSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoDespoilerCatalyzeFlagSystem _catalyze = default!;
    [Dependency] private readonly XenoDespoilerAcidSystem _acid = default!;

    private const float TileHalfExtent = 0.5f;
    private const float UnobstructedRangeBuffer = 1f;

    private EntityQuery<XenoDespoilerLingeringAcidComponent> _lingeringQuery;

    public override void Initialize()
    {
        _lingeringQuery = GetEntityQuery<XenoDespoilerLingeringAcidComponent>();

        SubscribeLocalEvent<XenoDespoilerComponent, XenoDespoilerCausticEmbraceActionEvent>(OnUse);
    }

    private void OnUse(Entity<XenoDespoilerComponent> ent, ref XenoDespoilerCausticEmbraceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<XenoDespoilerCausticEmbraceActionComponent>(args.Action, out var action))
            return;

        var ownerXform = Transform(ent);
        var ownerMap = _xform.ToMapCoordinates(ownerXform.Coordinates);
        var targetMap = _xform.ToMapCoordinates(args.Target);
        if (ownerMap.MapId != targetMap.MapId)
            return;

        var approach = targetMap.Position - ownerMap.Position;
        var dist = approach.Length();
        if (dist < 0.01f)
            return;

        var step = SnapDirectionToTile(approach / dist);
        if (step == Vector2.Zero)
            return;

        if (_catalyze.IsEmpowered(ent))
        {
            if (!CanEmpoweredLunge(ent, action, args, dist, out var victim))
                return;

            if (!_rmcActions.TryUseAction(args))
                return;

            ExecuteEmpoweredLunge(ent, action, victim.Value);
            _catalyze.TakeEmpowerment(ent);
            args.Handled = true;
            return;
        }

        var landing = ownerXform.Coordinates.Offset(step);

        if (_rmcMap.IsTileBlocked(landing) ||
            !_interaction.InRangeUnobstructed(ent, landing, range: action.NormalRange + UnobstructedRangeBuffer))
        {
            _popup.PopupClient(Loc.GetString("rmc-despoiler-pounce-blocked"), ent, ent);
            return;
        }

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        _xform.SetCoordinates(ent, landing);

        if (action.PounceSound is { } sound)
            _audio.PlayPvs(sound, ent);

        SpawnSplashAroundExceptBack(ent, action, landing, step);
    }

    private static Vector2 SnapDirectionToTile(Vector2 dir)
    {
        return new Vector2(Math.Sign(MathF.Round(dir.X)), Math.Sign(MathF.Round(dir.Y)));
    }

    private void SpawnSplashAroundExceptBack(EntityUid caster,
        XenoDespoilerCausticEmbraceActionComponent action,
        EntityCoordinates center,
        Vector2 forward)
    {
        var backX = -(int)forward.X;
        var backY = -(int)forward.Y;

        var centerMap = _xform.ToMapCoordinates(center);
        var hits = _lookup.GetEntitiesIntersecting(centerMap.MapId,
            Box2.CenteredAround(centerMap.Position, new Vector2(action.SplashScanSize, action.SplashScanSize)));

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                if (dx == backX && dy == backY)
                    continue;

                var tile = center.Offset(new Vector2(dx, dy));
                var tileMap = _xform.ToMapCoordinates(tile);

                var telegraph = PredictedSpawnAtPosition(action.TelegraphProto, tile);
                _hive.SetSameHive(caster, telegraph);

                foreach (var ent in hits)
                {
                    if (!IsValidVictim(ent, caster))
                        continue;

                    var entPos = _xform.ToMapCoordinates(Transform(ent).Coordinates).Position;
                    if (Math.Abs(entPos.X - tileMap.Position.X) > TileHalfExtent)
                        continue;

                    if (Math.Abs(entPos.Y - tileMap.Position.Y) > TileHalfExtent)
                        continue;

                    _damageable.TryChangeDamage(ent, action.SplashDamage, ignoreResistances: false, origin: caster);
                }

                if (_random.Prob(action.LingeringAcidChance))
                {
                    var puddle = PredictedSpawnAtPosition(action.LingeringAcidProto, tile);
                    _hive.SetSameHive(caster, puddle);

                    if (!_lingeringQuery.TryComp(puddle, out var puddleComp))
                        continue;

                    puddleComp.Caster = caster;
                    Dirty(puddle, puddleComp);
                }
            }
        }
    }

    private bool CanEmpoweredLunge(EntityUid uid,
        XenoDespoilerCausticEmbraceActionComponent action,
        XenoDespoilerCausticEmbraceActionEvent args,
        float dist,
        [NotNullWhen(true)] out EntityUid? victim)
    {
        victim = null;
        if (dist > action.EmpoweredRange)
        {
            _popup.PopupClient(Loc.GetString("rmc-despoiler-pounce-out-of-range"), uid, uid);
            return false;
        }

        victim = FindEmpoweredVictim(uid, args);
        if (victim is null)
        {
            _popup.PopupClient(Loc.GetString("rmc-despoiler-caustic-no-target"), uid, uid);
            return false;
        }

        if (!_interaction.InRangeUnobstructed(uid, victim.Value, range: action.EmpoweredRange + UnobstructedRangeBuffer))
        {
            _popup.PopupClient(Loc.GetString("rmc-despoiler-pounce-blocked"), uid, uid);
            victim = null;
            return false;
        }

        return true;
    }

    private void ExecuteEmpoweredLunge(Entity<XenoDespoilerComponent> ent,
        XenoDespoilerCausticEmbraceActionComponent action,
        EntityUid victim)
    {
        _xform.SetCoordinates(ent, Transform(victim).Coordinates);

        if (action.PounceSound is { } sound)
            _audio.PlayPredicted(sound, ent, ent);

        _damageable.TryChangeDamage(victim, action.EmpoweredDamage, ignoreResistances: false, origin: ent);
        _acid.ApplyAcid(victim, ent.AsNullable(), enhance: true);

        _stun.TryParalyze(victim, action.EmpoweredWeakenDuration, true);
    }

    private EntityUid? FindEmpoweredVictim(EntityUid caster, XenoDespoilerCausticEmbraceActionEvent args)
    {
        if (args.Entity is { } target && IsValidVictim(target, caster))
            return target;

        var landingMap = _xform.ToMapCoordinates(args.Target);
        foreach (var ent in _lookup.GetEntitiesIntersecting(landingMap.MapId,
                     Box2.CenteredAround(landingMap.Position, new Vector2(1f, 1f))))
        {
            if (IsValidVictim(ent, caster))
                return ent;
        }

        return null;
    }

    private bool IsValidVictim(EntityUid victim, EntityUid caster)
    {
        return victim != caster &&
               HasComp<MobStateComponent>(victim) &&
               !HasComp<XenoComponent>(victim);
    }
}
