using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Expires = TimeSpan.FromSeconds(4);
}
