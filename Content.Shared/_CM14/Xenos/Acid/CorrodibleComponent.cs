using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidSystem))]
public sealed partial class CorrodibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsCorrodible = true;
}
