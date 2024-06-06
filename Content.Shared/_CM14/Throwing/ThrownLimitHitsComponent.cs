using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Throwing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ThrowingSystem))]
public sealed partial class ThrownLimitHitsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hit;
}
