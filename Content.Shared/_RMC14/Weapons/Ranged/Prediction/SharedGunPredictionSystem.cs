using Content.Shared._RMC14.CCVar;
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

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session)
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

#pragma warning disable RA0002
        gun.ShootCoordinates = GetCoordinates(coordinates);
        gun.Target = GetEntity(target);
#pragma warning restore RA0002
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
