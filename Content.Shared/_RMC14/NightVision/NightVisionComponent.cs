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
            NightVisionColor.Orange => new Color(0.6f, 0.42f, 0.05f), // #9d6c0994
            NightVisionColor.White => new Color(0.35f, 0.35f, 0.35f),  // #595959
            NightVisionColor.Yellow => new Color(1.0f, 1.0f, 0.08f),    // #FFFF14
            NightVisionColor.Red => new Color(1.0f, 0.08f, 0.08f),     // #FF1414
            NightVisionColor.Blue => new Color(0.08f, 0.45f, 0.42f),    // #14736B
            _ => new Color(0.22f, 1.0f, 0.08f),                        // #39FF14 (Green/Default)
        };
    }
}
