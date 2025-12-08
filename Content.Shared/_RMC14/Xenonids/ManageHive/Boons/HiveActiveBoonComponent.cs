using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveActiveBoonComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;
}
