using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoEvolutionGranterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool GotOvipositorPopup;
}
