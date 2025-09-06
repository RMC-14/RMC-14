using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent]
public sealed partial class IntegratedVisorsComponent : Component
{
    [DataField]
    public List<EntProtoId> VisorsToAdd = new();

    [DataField]
    public bool StartToggled = false;
}
