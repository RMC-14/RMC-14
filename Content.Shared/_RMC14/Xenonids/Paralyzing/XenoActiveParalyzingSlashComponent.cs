using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Paralyzing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoParalyzingSlashSystem))]
public sealed partial class XenoActiveParalyzingSlashComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(4);

}
