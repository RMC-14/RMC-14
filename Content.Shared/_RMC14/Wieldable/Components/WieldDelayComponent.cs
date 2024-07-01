using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldDelayComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan BaseDelay = TimeSpan.FromSeconds(0.4);

    [DataField, AutoNetworkedField]
    public TimeSpan ModifiedDelay = TimeSpan.FromSeconds(0.4);
}
