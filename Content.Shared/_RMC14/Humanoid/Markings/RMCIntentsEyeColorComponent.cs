using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Humanoid.Markings;

/// <summary>
/// Changes the entity's eye color based on their "intent".
/// If they grab something, their eyes turn orange. If they are in harm mode, their eyes turn red, etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCIntentsEyeColorComponent : Component
{
    [DataField]
    public Color EyeColorHelp = Color.FromHex("#00ff00");

    [DataField]
    public Color EyeColorDisarm = Color.FromHex("#5a5afd");

    [DataField]
    public Color EyeColorGrab = Color.FromHex("#efa700");

    [DataField]
    public Color EyeColorHarm = Color.FromHex("#ff0000");

    [DataField]
    public Color DeadEyeColor = Color.FromHex("#000000");
}
