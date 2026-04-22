using Content.Shared._RMC14.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Vehicle;
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
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleRideSurfaceSystem _rideSurface = default!;

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session)
    {
        var user = session.AttachedEntity;

        if (user == null ||
            !_combatMode.IsInCombatMode(user) ||
            !_gun.TryGetGun(user.Value, out var ent, out var gun))
        {
            return null;
        }

        if (ent != GetEntity(netGun))
            return null;

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
        return _gun.AttemptShoot(user.Value, ent, gun, projectiles, session);
    }
}
