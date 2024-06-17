using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableWeaponModifiersSystem))]
public sealed partial class AttachableWeaponRangedModifiersComponent : Component
{
    [DataField("wielded", required:true), AutoNetworkedField]
    public AttachableWeaponModifierSet ModifiersWielded;
    
    [DataField("unwielded", required:true), AutoNetworkedField]
    public AttachableWeaponModifierSet ModifiersUnwielded;
}
