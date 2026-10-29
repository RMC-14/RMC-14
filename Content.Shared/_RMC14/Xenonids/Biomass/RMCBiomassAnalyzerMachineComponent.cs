using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCBiomassAnalyzerMachineSystem))]
public sealed partial class RMCBiomassAnalyzerMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OrganContainerId = "RMCBiomassOrganQueue";

    [DataField, AutoNetworkedField]
    public int MaxQueueLength = 10;

    [DataField]
    public SoundSpecifier? NotifySound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan PrintingDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan QueuePrintDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? PrintingUntil;

    [DataField, AutoNetworkedField]
    public int ProcessingValue;

    [DataField, AutoNetworkedField]
    public bool AutoProcessOrgan;

    [ViewVariables, AutoNetworkedField]
    public List<RMCBiomassQueueJob> Queue = new();

    [DataField, AutoNetworkedField]
    public bool QueueProcessing;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextQueueItemAt;
}

[Serializable, NetSerializable]
public readonly record struct RMCBiomassQueueJob(EntProtoId<RMCBiomassUpgradeComponent>? Upgrade, NetEntity? Organ);
