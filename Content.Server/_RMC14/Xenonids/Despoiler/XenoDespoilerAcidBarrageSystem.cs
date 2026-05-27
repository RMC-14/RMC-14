using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidBarrageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectile = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoDespoilerCatalyzeFlagSystem _catalyze = default!;

    private EntityQuery<XenoDespoilerComponent> _despoilerQuery;
    private EntityQuery<XenoDespoilerArmedBarrageComponent> _armedQuery;
    private EntityQuery<XenoDespoilerChargingBarrageComponent> _chargingQuery;
    private EntityQuery<XenoDespoilerAcidBarrageProjectileComponent> _projectileQuery;

    public override void Initialize()
    {
        _despoilerQuery = GetEntityQuery<XenoDespoilerComponent>();
        _armedQuery = GetEntityQuery<XenoDespoilerArmedBarrageComponent>();
        _chargingQuery = GetEntityQuery<XenoDespoilerChargingBarrageComponent>();
        _projectileQuery = GetEntityQuery<XenoDespoilerAcidBarrageProjectileComponent>();

        SubscribeLocalEvent<XenoDespoilerComponent, XenoDespoilerAcidBarrageActionEvent>(OnAction);
        SubscribeNetworkEvent<XenoDespoilerBarrageStartChargeRequest>(OnStartChargeRequest);
        SubscribeNetworkEvent<XenoDespoilerBarrageFireRequest>(OnFireRequest);
    }

    private void OnAction(EntityUid uid, XenoDespoilerComponent comp, XenoDespoilerAcidBarrageActionEvent args)
    {
        if (args.Handled || !HasComp<XenoDespoilerAcidBarrageActionComponent>(args.Action))
            return;

        if (_armedQuery.HasComp(uid))
        {
            ResetBarrage(uid, args.Action);
            args.Handled = true;
            return;
        }

        EnsureComp<XenoDespoilerArmedBarrageComponent>(uid);
        _actions.SetToggled(args.Action.Owner, true);
        _popup.PopupEntity(Loc.GetString("rmc-despoiler-barrage-armed"), uid, uid);
        args.Handled = true;
    }

    private void OnStartChargeRequest(XenoDespoilerBarrageStartChargeRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!_despoilerQuery.HasComp(uid) || !_armedQuery.HasComp(uid) || _chargingQuery.HasComp(uid))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            return;

        if (!TryGetBarrageAction(uid, out _, out var action))
            return;

        var charging = EnsureComp<XenoDespoilerChargingBarrageComponent>(uid);
        charging.StartedAt = _timing.CurTime;
        charging.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(action.MaxChargeSeconds);
        charging.Empowered = _catalyze.IsEmpowered(uid, Comp<XenoDespoilerComponent>(uid));
        charging.Target = msg.Target;
        charging.SpeedMultiplier = action.ChargingSpeedMultiplier;
        Dirty(uid, charging);

        if (action.ChargeSound is { } sound)
            _audio.PlayPvs(sound, uid);
    }

    private void OnFireRequest(XenoDespoilerBarrageFireRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryComp<XenoDespoilerComponent>(uid, out var comp))
            return;

        if (!_chargingQuery.TryComp(uid, out var charge))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
        {
            ResetBarrage(uid);
            return;
        }

        var coords = GetCoordinates(msg.Target);
        if (!coords.IsValid(EntityManager))
            coords = GetCoordinates(charge.Target);

        if (TryGetBarrageAction(uid, out var actionEnt, out var action) &&
            _rmcActions.TryUseAction(uid, actionEnt.Owner, uid))
        {
            FireVolley(uid, action, charge, coords);
            _actions.SetCooldown((actionEnt.Owner, null), action.PostFireCooldown);
            _catalyze.TakeEmpowerment(uid, comp);
        }

        ResetBarrage(uid);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerChargingBarrageComponent>();
        while (query.MoveNext(out var uid, out var charge))
        {
            if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            {
                ResetBarrage(uid);
                continue;
            }

            if (!TryGetBarrageAction(uid, out _, out var action))
                continue;

            if (now >= charge.ExpiresAt + action.ChargeGracePeriod)
                ResetBarrage(uid);
        }
    }

    private void ResetBarrage(EntityUid uid, EntityUid? actionEnt = null)
    {
        RemCompDeferred<XenoDespoilerChargingBarrageComponent>(uid);
        RemCompDeferred<XenoDespoilerArmedBarrageComponent>(uid);

        if (actionEnt is null && TryGetBarrageAction(uid, out var found, out _))
            actionEnt = found.Owner;

        if (actionEnt is { } id)
            _actions.SetToggled(id, false);
    }

    private bool TryGetBarrageAction(EntityUid xeno,
        out Entity<ActionComponent> actionEnt,
        out XenoDespoilerAcidBarrageActionComponent action)
    {
        foreach (var entry in _rmcActions.GetActionsWithEvent<XenoDespoilerAcidBarrageActionEvent>(xeno))
        {
            if (!TryComp(entry.Owner, out XenoDespoilerAcidBarrageActionComponent? barrage))
                continue;

            actionEnt = entry;
            action = barrage;
            return true;
        }

        actionEnt = default;
        action = default!;
        return false;
    }

    private void FireVolley(EntityUid uid, XenoDespoilerAcidBarrageActionComponent action,
        XenoDespoilerChargingBarrageComponent charge, EntityCoordinates target)
    {
        var heldFor = (float)(_timing.CurTime - charge.StartedAt).TotalSeconds;
        var chargeFrac = Math.Clamp(heldFor / action.MaxChargeSeconds, 0f, 1f);

        var count = (int)MathF.Round(MathHelper.Lerp(action.MinProjectiles, action.MaxProjectiles, chargeFrac));
        count = Math.Clamp(count, action.MinProjectiles, action.MaxProjectiles);
        if (charge.Empowered)
            count += action.EmpowerBonusProjectiles;

        var casterCoords = Transform(uid).Coordinates;
        var casterMap = _xform.ToMapCoordinates(casterCoords);
        var targetMap = _xform.ToMapCoordinates(target);

        Vector2 aimDir;
        float baseAngle;
        if (casterMap.MapId == targetMap.MapId &&
            (targetMap.Position - casterMap.Position).LengthSquared() >= 0.0001f)
        {
            var mapAim = targetMap.Position - casterMap.Position;
            baseAngle = MathF.Atan2(mapAim.Y, mapAim.X);
            aimDir = Vector2.Normalize(mapAim);
        }
        else
        {
            var fallback = Transform(uid).LocalRotation.ToWorldVec();
            baseAngle = MathF.Atan2(fallback.Y, fallback.X);
            aimDir = Vector2.Normalize(fallback);
        }

        var spawnCoords = casterCoords.Offset(aimDir);
        var scatterRad = MathHelper.DegreesToRadians(action.ScatterDegrees);
        var scaleSpan = action.MaxProjectileScale - action.MinProjectileScale;

        for (var i = 0; i < count; i++)
        {
            var angle = baseAngle + ((float)_random.NextDouble() * 2f - 1f) * scatterRad;
            var unit = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var rangeTiles = _random.Next(action.MinRangeTiles, action.MaxRangeTiles + 1);

            var proj = Spawn(action.ProjectileId, spawnCoords);
            _hive.SetSameHive(uid, proj);

            if (_projectileQuery.TryComp(proj, out var projComp))
            {
                projComp.Shooter = uid;
                projComp.LingeringAcidChance = action.LingeringAcidChance;
                var scaleFactor = action.MinProjectileScale + (float)_random.NextDouble() * scaleSpan;
                projComp.Scale = new Vector2(scaleFactor, scaleFactor);
                Dirty(proj, projComp);
            }

            _rmcProjectile.SetMaxRange(proj, rangeTiles);
            _gun.ShootProjectile(proj, unit * rangeTiles, Vector2.Zero, uid, uid, speed: action.ProjectileSpeed);
        }

        if (action.FireSound is { } sound)
            _audio.PlayPvs(sound, uid);
    }
}
