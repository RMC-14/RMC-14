using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableIFFSystem))]
public sealed partial class GunAttachableIFFComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool PreventFriendlyFire;
}
