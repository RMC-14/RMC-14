using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrapParasiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan JumpTime = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan DisableTime = TimeSpan.FromSeconds(0.25);

    [DataField, AutoNetworkedField]
    public TimeSpan? LeapAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? DisableAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NormalLeapDelay;
}
