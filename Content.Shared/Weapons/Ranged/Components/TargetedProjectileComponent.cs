using Content.Shared._RMC14.Weapons.Ranged.AimedShot;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem), typeof(XenoProjectileSystem), typeof(SharedRMCAimedShotSystem))]
public sealed partial class TargetedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;
}
