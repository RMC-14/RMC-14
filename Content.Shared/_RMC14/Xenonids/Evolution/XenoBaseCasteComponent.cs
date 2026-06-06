using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoBaseCasteComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
