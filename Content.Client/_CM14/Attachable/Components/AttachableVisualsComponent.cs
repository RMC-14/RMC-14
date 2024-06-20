namespace Content.Client._CM14.Attachable.Components;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderVisualsSystem))]
public sealed partial class AttachableVisualsComponent : Component
{
    /// <summary>
    ///     The path to the RSI file that contains all the attached states.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Rsi = string.Empty;

    /// <summary>
    ///     This prefix is added to the name of the slot the attachable is installed in.
    ///     The prefix must be in kebab-case and end with a dash, like so: example-prefix-
    ///     The RSI must contain a state for every slot the attachable fits into, named in
    ///     this pattern: prefix-slot-name
    ///     The slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Prefix = string.Empty;

    //If this is toggled on and the item has an AttachableToggleableComponent, then the visualisation system will try to show a different sprite when it's active.
    //Each active state must have "-active" appended to the end of its name.
    [DataField, AutoNetworkedField]
    public bool ShowActive;
}
