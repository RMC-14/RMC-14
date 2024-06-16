using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using System.Numerics;


namespace Content.Client._CM14.Attachable;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderVisualsSystem))]
public sealed partial class AttachableHolderVisualsComponent : Component
{
    //This dictionary contains a list of offsets for every slot that should display the attachable placed into it.
    //If a slot is not in this dictionary, the attachable inside will not be displayed.
    //The list of valid slot names can be found in AttachableHolderComponent.cs
    [DataField("offsets", required:true), AutoNetworkedField]
    public Dictionary<string, Vector2> Offsets;
}
