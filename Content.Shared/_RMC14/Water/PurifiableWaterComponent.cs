using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWaterSystem))]
public sealed partial class PurifiableWaterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Toxic;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5);
}

[Serializable, NetSerializable]
public enum PurifiableWaterLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum PurifiableWaterVisuals
{
    Toxic,
    Purified,
}
