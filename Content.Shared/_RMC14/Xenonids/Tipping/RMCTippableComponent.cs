using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Tipping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCTippingSystem))]
public sealed partial class RMCTippableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan BigDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan SmallDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public bool IsTipped = false;
}
