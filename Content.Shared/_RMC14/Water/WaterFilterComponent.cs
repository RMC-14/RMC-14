using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCWaterSystem))]
public sealed partial class WaterFilterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public bool Triggered;

    [DataField, AutoNetworkedField]
    public TimeSpan TriggerDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan ResetDelay = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public int OneOffLoad = 5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TriggerAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ResetAt;
}

[Serializable, NetSerializable]
public enum WaterFilterVisuals
{
    Active,
}

[Serializable, NetSerializable]
public enum WaterFilterLayers
{
    Base,
}
