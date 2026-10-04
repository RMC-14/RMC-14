using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Centrifuge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCCentrifugeSystem))]
public sealed partial class RMCCentrifugeComponent : Component
{
    [DataField, AutoNetworkedField]
    public string InputSlotId = "centrifuge_input_slot";

    [DataField, AutoNetworkedField]
    public string OutputBoxSlotId = "centrifuge_output_box_slot";

    [DataField, AutoNetworkedField]
    public CentrifugeMode Mode = CentrifugeMode.Split;

    [DataField, AutoNetworkedField]
    public CentrifugeInputSource InputSource = CentrifugeInputSource.Container;

    [DataField, AutoNetworkedField]
    public string? Label;

    [DataField, AutoNetworkedField]
    public EntityUid? TuringDispenser;

    [DataField, AutoNetworkedField]
    public float TetherRange = 20;

    [DataField, AutoNetworkedField]
    public bool Spinning;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FinishAt;

    [DataField, AutoNetworkedField]
    public TimeSpan SpinDuration = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RequestRunSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpinSound = new SoundPathSpecifier("/Audio/Machines/spinning.ogg");
}
