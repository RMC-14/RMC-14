using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoRaffleCandidateComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Target;

    [DataField, AutoNetworkedField]
    public int Tier;

    [DataField, AutoNetworkedField]
    public bool Leapfrog;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? GraceUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastFullBlockNotify;

    [DataField, AutoNetworkedField]
    public bool Evolving;

    [DataField, AutoNetworkedField]
    public FixedPoint2? OriginalMax;
}
