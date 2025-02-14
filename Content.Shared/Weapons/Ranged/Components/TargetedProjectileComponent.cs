using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem), typeof(XenoProjectileSystem))]
public sealed partial class TargetedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    [DataField, AutoNetworkedField]
    public EntityCoordinates? TargetCoordinates;
}
