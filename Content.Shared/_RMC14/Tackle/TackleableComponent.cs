using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpireAfter = TimeSpan.FromSeconds(4);
}
