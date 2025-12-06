using Content.Shared._RMC14.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
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

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session)
    {
        var user = session.AttachedEntity;

        var ent = new EntityUid();
        var gun = new GunComponent();

        if (user != null && _combatMode.IsInCombatMode(user))
        {
            if (_gun.TryGetGun(user.Value, out var inputent, out var inputgun) &&
                inputent.Id == netGun.Id)
            {
                 ent = inputent;
                 gun = inputgun;
            }
            else if (_gun.TryGetAkimboGun(user.Value, out var akimboent, out var akimbogun) &&
                     akimboent.Id == netGun.Id )
            {
                 ent = akimboent;
                 gun = akimbogun;
            }
        }
        else
        {
            return null;
        }

        if (ent != GetEntity(netGun))
            return null;

#pragma warning disable RA0002
        gun.ShootCoordinates = GetCoordinates(coordinates);
        gun.Target = GetEntity(target);
#pragma warning restore RA0002
        return _gun.AttemptShoot(user.Value, ent, gun, projectiles, session);
    }
}
