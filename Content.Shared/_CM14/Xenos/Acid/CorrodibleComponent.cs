using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidSystem))]
public sealed partial class CorrodibleComponent : Component
{
    // TODO CM14 intel and nuke shouldn't be corrodible
    [DataField, AutoNetworkedField]
    public bool IsCorrodible = true;
}
