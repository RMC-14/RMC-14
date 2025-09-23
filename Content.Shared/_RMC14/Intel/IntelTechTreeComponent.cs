using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.FixedPoint;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(IntelSystem), typeof(TechSystem))]
public sealed partial class IntelTechTreeComponent : Component
{
    [DataField, AutoNetworkedField]
    public IntelTechTree Tree = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 ColonyCommunicationsPoints = FixedPoint2.New(0.7);

    [DataField, AutoNetworkedField]
    public FixedPoint2 PowerPoints = FixedPoint2.New(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastAnnounceAt;

    [DataField, AutoNetworkedField]
    public FixedPoint2 LastAnnouncePoints;

    [DataField, AutoNetworkedField]
    public bool DoAnnouncements;

    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> AnnounceIn = new() { "MarineCommand", "MarineIntel" };

    [DataField, AutoNetworkedField]
    public int HumanoidCorpses;
}
