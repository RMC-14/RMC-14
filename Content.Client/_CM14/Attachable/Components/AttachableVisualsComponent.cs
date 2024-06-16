using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;


namespace Content.Client._CM14.Attachable;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderVisualsSystem))]
public sealed partial class AttachableVisualsComponent : Component
{
    //The path to the RSI file that contains all the attached states.
    [DataField("rsi", required:true), AutoNetworkedField]
    public string Rsi;
    
    //This prefix is added to the name of the slot the attachable is installed in.
    //The prefix must be in kebab-case and end with a dash, like so: example-prefix-
    //The RSI must contain a state for every slot the attachable fits into, named in this pattern: prefix-slot-name
    //The slot names can be found in AttachableHolderComponent.cs
    [DataField("prefix", required:true), AutoNetworkedField]
    public string Prefix;
}
