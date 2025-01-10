using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VisorSystem))]
public sealed partial class VisorComponent : Component
{
    [DataField]
    public ComponentRegistry? Add;
}
