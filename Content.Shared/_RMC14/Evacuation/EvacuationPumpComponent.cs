using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class EvacuationPumpComponent : Component
{
    [DataField("activeSound", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ActiveSound = default!;
}

[Serializable, NetSerializable]
public enum EvacuationPumpLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum EvacuationPumpVisuals
{
    Empty,
    TwentyFive,
    Fifty,
    SeventyFive,
    Full,
}

