using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HivePylonComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Tower;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRoyalResin;
}
