using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.AciderGeneration;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoAciderGenerationComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenGeneration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextIncrease;

    [DataField, AutoNetworkedField]
    public int IncreaseAmount = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireDuration = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public TimeSpan? ExpireAt;
}

[Serializable, NetSerializable]
public enum XenoAcidGeneratingVisuals
{
    Generating,
    Downed,
    Resting,
}

[Serializable, NetSerializable]
public enum XenoAcidGeneratingVisualLayers : byte
{
    Base,
}
