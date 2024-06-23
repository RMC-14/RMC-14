using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidSystem))]
public sealed partial class CorrodibleComponent : Component
{
    // TODO RMC14 intel and nuke shouldn't be corrodible
    [DataField, AutoNetworkedField]
    public bool IsCorrodible = true;
}
