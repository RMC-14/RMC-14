using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class XenoConstructFramesComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Frames = new();
}
