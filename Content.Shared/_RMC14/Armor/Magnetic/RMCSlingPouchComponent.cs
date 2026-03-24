using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCSlingPouchComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Item;
}
