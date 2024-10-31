using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.MotionDetector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunMotionDetectorSystem))]
public sealed partial class GunToggleableMotionDetectorComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BatteryDrain = 0.45f;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleMotionDetector";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}
