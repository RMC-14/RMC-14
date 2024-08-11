using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipTerminalWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public DropshipTerminalWeaponsScreen ScreenOne;

    [DataField, AutoNetworkedField]
    public DropshipTerminalWeaponsScreen ScreenTwo;
}

[Serializable, NetSerializable]
public enum DropshipTerminalWeaponsUi
{
    Key,
}

[Serializable, NetSerializable]
public enum DropshipTerminalWeaponsScreen
{
    Main = 0,
    Equip,
    Target,
    Strike,
    Weapon,
}
