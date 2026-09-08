using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunSecondWindComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HasSecondWind = true;
}
