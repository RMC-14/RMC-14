using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Client._CM14.Attachable.Components;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderVisualsSystem))]
public sealed partial class AttachableVisualsComponent : Component
{
    /// <summary>
    ///     Optional, only used if the item's own state should not be used.
    ///     The path to the RSI file that contains all the attached states.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath? Rsi;

    /// <summary>
    ///     Optional, only used if the item's own state should not be used.
    ///     This prefix is added to the name of the slot the attachable is installed in.
    ///     The prefix must be in kebab-case and end with a dash, like so: example-prefix-
    ///     The RSI must contain a state for every slot the attachable fits into, named in
    ///     this pattern: prefix-slot-name
    ///     The slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Prefix;

    /// <summary>
    ///     Optional, only used if the item's own state should not be used.
    ///     This prefix is added to the name of the slot the attachable is installed in.
    ///     The prefix must be in kebab-case and end with a dash, like so: example-prefix-
    ///     The RSI must contain a state for every slot the attachable fits into, named in
    ///     this pattern: prefix-slot-name
    ///     The slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Suffix = "_a";

    /// <summary>
    ///     If true, will include the holder's slot name to find this attachment's state
    ///     in its RSI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IncludeSlotName;

    /// <summary>
    ///     If this is toggled on and the item has an AttachableToggleableComponent, then the visualisation system will try to show a different sprite when it's active.
    ///     Each active state must have "-active" appended to the end of its name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowActive;

    [DataField, AutoNetworkedField]
    public int Layer;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;
}
