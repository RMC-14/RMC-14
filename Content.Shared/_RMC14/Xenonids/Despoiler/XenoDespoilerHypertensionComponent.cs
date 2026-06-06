using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoDespoilerHypertensionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxStacks = 4;

    [DataField, AutoNetworkedField]
    public int Stacks;

    [DataField, AutoNetworkedField]
    public float Points;

    [DataField]
    public float PointsPerStack = 200f;

    [DataField]
    public float PointsPerSlash = 100f;

    [DataField]
    public float PointsPerDamageTaken = 0.5f;

    [DataField]
    public TimeSpan DecayDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public float DecayPerSecond = 200f;

    [DataField]
    public float BonusBurnPerStack = 10f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastActivityAt;
}
