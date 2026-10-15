using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Requisitions.Components;

[Serializable, NetSerializable]
public enum RMCBlackMarketScannerVisuals
{
    Scanning,
    Value,
}

[Serializable, NetSerializable]
public enum RMCBlackMarketScannerLayers
{
    Clamp,
    Value,
}

[Serializable, NetSerializable]
public enum RMCBlackMarketScannerValueVisual
{
    Red,
    Orange,
    Yellow,
    Green,
    Cyan,
    White,
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RMCBlackMarketScannerComponent : Component
{
    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    public bool Scanning;

    public TimeSpan ScanEndsAt;
}
