using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWaterSystem))]
public sealed partial class PurifiableWaterComponent : Component
{
    [DataField, AutoNetworkedField]
    public PurifiableWaterState State = PurifiableWaterState.Toxic;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan PurifyDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float SloshChance = 0.3f;

    [DataField, AutoNetworkedField]
    public float ThrowChance = 0.7f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier SloshSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}

[Serializable, NetSerializable]
public enum PurifiableWaterState
{
    Toxic,
    Dispersing,
    Purified,
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
    Dispersing,
    Purified,
}
