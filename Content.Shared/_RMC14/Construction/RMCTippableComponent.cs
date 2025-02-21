using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCConstructionSystem))]
public sealed partial class RMCTippableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan BigDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan SmallDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public bool IsTipped = false;
}
