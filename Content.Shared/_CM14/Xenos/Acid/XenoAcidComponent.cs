using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidSystem))]
public sealed partial class XenoAcidComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);
}
