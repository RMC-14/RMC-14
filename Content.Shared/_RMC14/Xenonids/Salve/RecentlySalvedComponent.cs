using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecentlySalvedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}

[Serializable, NetSerializable]
public enum XenoHealerVisuals
{
    Gooped,
}

[Serializable, NetSerializable]
public enum XenoHealerVisualLayers
{
    Goop,
}
