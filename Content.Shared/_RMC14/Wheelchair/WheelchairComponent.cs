using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Wheelchair;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WheelchairSystem))]
public sealed partial class WheelchairComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.0f;

    [DataField, AutoNetworkedField]
    public EntProtoId? BellAction;

    [DataField, AutoNetworkedField]
    public SoundSpecifier BellSound = new SoundPathSpecifier("/Audio/Items/desk_bell_ring.ogg");
}
