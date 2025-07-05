using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class XenoParasiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ManualAttachDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan SelfAttachDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromMinutes(1.5);

    [DataField, AutoNetworkedField]
    public float InfectRange = 1.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? InfectedVictim;

    /// <summary>
    ///     How long it takes for the parasite to fall off the victim's mask, finishing the infecting process.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FallOffDelay = TimeSpan.FromSeconds(15);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? FallOffAt;

    [DataField, AutoNetworkedField]
    public bool FellOff;
}
