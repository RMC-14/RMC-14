namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Allows a synth to rename itself through the K9 name changer item.
/// </summary>
[RegisterComponent]
public sealed partial class RMCK9NameChangerComponent : Component
{
    /// <summary>
    /// Maximum accepted name length from the dialog.
    /// </summary>
    [DataField]
    public int MaxNameLength = 32;
}
