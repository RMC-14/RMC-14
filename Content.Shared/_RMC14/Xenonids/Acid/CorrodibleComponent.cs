using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoAcidSystem))]
public sealed partial class CorrodibleComponent : Component
{
    // TODO RMC14 intel and nuke shouldn't be corrodible
    [DataField, AutoNetworkedField]
    public bool IsCorrodible = true;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeToApply = TimeSpan.FromSeconds(4);
}
