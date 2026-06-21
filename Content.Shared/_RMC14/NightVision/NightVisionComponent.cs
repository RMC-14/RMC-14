using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    [DataField, AutoNetworkedField]
    public NightVisionState State = NightVisionState.Full;

    [DataField, AutoNetworkedField]
    public bool Overlay;

    [DataField, AutoNetworkedField]
    public bool Innate;

    [DataField, AutoNetworkedField]
    public bool SeeThroughContainers;

    [DataField, AutoNetworkedField]
    public bool Green;

    [DataField, AutoNetworkedField]
    public bool Mesons;

    [DataField, AutoNetworkedField]
    public bool BlockScopes;

    [DataField, AutoNetworkedField]
    public bool OnlyHalf;
}

[Serializable, NetSerializable]
public enum NightVisionState
{
    Off,
    Half,
    Full,
}

[Serializable, NetSerializable]
public enum NightVisionColor
{
    Green,
    Orange,
    White,
    Yellow,
    Red,
    Blue,
}

public static class NightVisionColorExtensions
{
    public static Color ToColor(this NightVisionColor color)
    {
        return color switch
        {
            NightVisionColor.Orange => new Color(1.0f, 0.8f, 0.4f),
            NightVisionColor.White => new Color(0.83f, 0.83f, 0.83f),
            NightVisionColor.Yellow => new Color(1.0f, 1.0f, 0.4f),
            NightVisionColor.Red => new Color(1.0f, 0.2f, 0.2f),
            NightVisionColor.Blue => new Color(0.4f, 0.8f, 1.0f),
            _ => new Color(0.22f, 1.0f, 0.08f),
        };
    }
}
