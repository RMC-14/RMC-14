using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoAcidSystem))]
public sealed partial class XenoAcidComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);
}
