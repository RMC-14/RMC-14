using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BulletholeSystem))]
public sealed partial class BulletholeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BulletholeCount = 0;

    [DataField, AutoNetworkedField]
    public int BulletholeState;
}

[Serializable, NetSerializable]
public enum BulletholeVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum BulletholeVisualsLayers : byte
{
    Bullethole,
}
