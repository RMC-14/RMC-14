using Content.Shared.FixedPoint;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ManageHiveSystem))]
public sealed partial class ManageHiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 DevolvePlasmaCost = 500;

    [DataField, AutoNetworkedField]
    public FixedPoint2 JellyPlasmaCost = 500;

    [DataField, AutoNetworkedField]
    public ProtoId<PlayTimeTrackerPrototype> PlayTime = "CMJobXenoQueen";

    [DataField, AutoNetworkedField]
    public TimeSpan JellyRequiredTime = TimeSpan.FromHours(25);
}
