using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipTargetEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
