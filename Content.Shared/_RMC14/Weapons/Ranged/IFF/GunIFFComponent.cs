using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunIFFSystem))]
public sealed partial class GunIFFComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Intrinsic;

    [DataField, AutoNetworkedField]
    public bool Enabled;
}
