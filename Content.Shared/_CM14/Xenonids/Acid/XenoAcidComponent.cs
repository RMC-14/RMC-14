using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidSystem))]
public sealed partial class XenoAcidComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);
}
