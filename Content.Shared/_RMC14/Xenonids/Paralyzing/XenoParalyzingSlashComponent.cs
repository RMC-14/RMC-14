using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paralyzing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoParalyzingSlashSystem))]
public sealed partial class XenoParalyzingSlashComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan StunDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4);
}
