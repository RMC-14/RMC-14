using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

public abstract class SharedGunPredictionSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleRideSurfaceSystem _rideSurface = default!;

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session, bool rearmSemiAuto = false)
    {
        var user = session.AttachedEntity;
        var gunUid = GetEntity(netGun);

        if (user == null ||
            !_combatMode.IsInCombatMode(user) ||
            !TryComp(gunUid, out GunComponent? gun) ||
            !CanRequestGun(user.Value, gunUid))
        {
            return null;
        }

        var shootCoordinates = GetCoordinates(coordinates);
        var targetUid = GetEntity(target);
        if (targetUid is { } clickedTarget)
        {
            var mapCoordinates = _transform.ToMapCoordinates(shootCoordinates);
            if (_rideSurface.TryGetRiderAtCoordinates(clickedTarget, mapCoordinates, out var rider))
                targetUid = rider;
        }

#pragma warning disable RA0002
        gun.ShootCoordinates = shootCoordinates;
        gun.Target = targetUid;
#pragma warning restore RA0002
        if (rearmSemiAuto)
            _gun.ResetShotCounter(gunUid, gun);

        return _gun.AttemptShoot(user.Value, gunUid, gun, projectiles, session);
    }

    private bool CanRequestGun(EntityUid user, EntityUid gun)
    {
        if (_gun.TryGetGun(user, out var activeGun, out _) &&
            activeGun == gun)
        {
            return true;
        }

        return TryComp(user, out HandsComponent? hands) &&
               _hands.IsHolding((user, hands), gun);
    }
}
