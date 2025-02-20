using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCConstructionSystem))]
public sealed partial class RMCTippableComponent : Component
{
    [DataField("isTipped", required: false), AutoNetworkedField]
    public bool IsTipped = false;
}
