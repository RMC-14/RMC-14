using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBulkyArmorSystem))]
public sealed partial class RMCBulkyArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsBulky = true;
}
