using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.CPR;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CPRSystem))]
public sealed partial class ReceivingCPRComponent : Component
{
    // The time it takes to perform CPR pre skill modification.
    [DataField, AutoNetworkedField]
    public int CPRPerformingTime = 4;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;
}
