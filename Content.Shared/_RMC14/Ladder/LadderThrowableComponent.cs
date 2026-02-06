using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ladder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LadderThrowableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DoAfterDuration = 1f;
}
