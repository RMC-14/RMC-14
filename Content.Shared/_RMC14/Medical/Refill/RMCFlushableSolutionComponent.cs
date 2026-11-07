using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class RMCFlushableSolutionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Solution;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan FlushTime;
}
