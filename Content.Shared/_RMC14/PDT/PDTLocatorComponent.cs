using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PDT;

[RegisterComponent]
public sealed partial class PDTLocatorComponent : Component
{
    [DataField]
    public EntityUid? LinkedBracelet;

    [DataField]
    public string? Serial;

    [DataField]
    public float PingCharge = 35f;

    [DataField]
    public SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/tick.ogg", AudioParams.Default.WithMaxDistance(5f));
}

[Serializable, NetSerializable]
public enum PDTLocatorVisuals
{
    Screen,
    Bracelet,
}

[Serializable, NetSerializable]
public enum PDTLocatorScreenVisuals
{
    Off,
    Normal,
    Yellow,
    Red,
}

[Serializable, NetSerializable]
public enum PDTLocatorBraceletVisuals
{
    Hidden,
    Linked,
    Unlinked,
}

[Serializable, NetSerializable]
public enum PDTLocatorVisualLayers
{
    Screen,
    Bracelet,
}
