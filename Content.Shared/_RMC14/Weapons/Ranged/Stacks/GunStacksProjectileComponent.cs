using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunStacksSystem))]
public sealed partial class GunStacksProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Gun;
}
