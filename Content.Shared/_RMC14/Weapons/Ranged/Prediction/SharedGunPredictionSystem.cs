using Content.Shared._RMC14.CCVar;
using Content.Shared.CombatMode;
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

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session, bool rearmSemiAuto = false)
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

#pragma warning disable RA0002
        gun.ShootCoordinates = GetCoordinates(coordinates);
        gun.Target = GetEntity(target);
#pragma warning restore RA0002
        var shouldRearmSemiAuto =
            rearmSemiAuto &&
            gun.SelectedMode == SelectiveFire.SemiAuto &&
            !HasComp<GunClickToFireComponent>(ent);

        if (shouldRearmSemiAuto)
            _gun.ResetShotCounter(ent, gun);

        // Dont snapp the cooldown forward to the current frame on every generated rearm
        return _gun.AttemptShoot(user.Value, ent, gun, projectiles, session, shouldRearmSemiAuto);
    }
}
