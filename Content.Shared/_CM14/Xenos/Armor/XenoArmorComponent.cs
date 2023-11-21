using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoArmorSystem))]
public sealed partial class XenoArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Armor;
}
