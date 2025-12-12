using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCSlingPouchItemComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityUid Pouch;
}
