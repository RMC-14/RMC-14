using Content.Client._RMC14.Attachable.Systems;
using System.Numerics;

namespace Content.Client._RMC14.Attachable.Components;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderVisualsSystem))]
public sealed partial class AttachableHolderVisualsComponent : Component
{
    /// <summary>
    ///     This dictionary contains a list of offsets for every slot that should display the attachable placed into it.
    ///     If a slot is not in this dictionary, the attachable inside will not be displayed.
    ///     The list of valid slot names can be found in AttachableHolderComponent.cs
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<string, Vector2> Offsets = new();
}
