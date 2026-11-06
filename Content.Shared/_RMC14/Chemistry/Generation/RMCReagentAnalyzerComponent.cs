using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCReagentAnalyzerSystem))]
public sealed partial class RMCReagentAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlotId = "reagent_analyzer_sample_slot";

    [DataField]
    public FixedPoint2 RequiredVolume = FixedPoint2.New(30);

    [DataField, AutoNetworkedField]
    public int SampleNumber = 1;

    [DataField]
    public TimeSpan ProcessDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public SoundSpecifier ProcessSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/fax.ogg");

    [DataField]
    public TimeSpan ProcessSoundLead = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
}

[Serializable, NetSerializable]
public enum RMCReagentAnalyzerVisualState : byte
{
    Idle,
    Sample,
    Processing,
    Finished,
    Failed,
    Error,
}

[Serializable, NetSerializable]
public enum RMCReagentAnalyzerVisuals : byte
{
    State,
}
