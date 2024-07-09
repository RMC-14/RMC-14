using Content.Client._RMC14.Attachable.Systems;
using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Attachable.Components;

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
    ///     The RSI must contain a state for every slot the attachable fits into.
    ///     If the attachment only fits into one slot, it should be named as follows: normal-state_suffix.
    ///     The slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Prefix;

    /// <summary>
    ///     Optional, only used if the item's own state should not be used.
    ///     This suffix is added to the name of the slot the attachable is installed in.
    ///     The RSI must contain a state for every slot the attachable fits into.
    ///     If the attachment only fits into one slot, it should be named as follows: normal-state_suffix.
    ///     The slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Suffix = "_a";

    /// <summary>
    ///     If true, will include the holder's slot name to find this attachment's state
    ///     in its RSI.
    ///     In this case, there must be a separate state for each slot the attachment fits into.
    ///     The states should be named as follows: prefix-slot-name-suffix.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IncludeSlotName;

    /// <summary>
    ///     If this is toggled on and the item has an AttachableToggleableComponent, then the visualisation system will try to show a different sprite when it's active.
    ///     Each active state must have "-on" appended to the end of its name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowActive;

    /// <summary>
    ///     If this is set to true, the attachment will be redrawn on its holder every time it receives an AppearanceChangeEvent. Useful for things like the UGL.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RedrawOnAppearanceChange;

    [DataField, AutoNetworkedField]
    public int Layer;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;
}
