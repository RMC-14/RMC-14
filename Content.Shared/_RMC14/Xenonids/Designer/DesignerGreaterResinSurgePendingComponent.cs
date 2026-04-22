using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(DesignerGreaterResinSurgeSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerGreaterResinSurgePendingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Designer;

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public NetCoordinates Origin;

    [DataField, AutoNetworkedField]
    public EntityUid Grid;

    [DataField, AutoNetworkedField]
    public float Range;

    [DataField, AutoNetworkedField]
    public EntProtoId AnimationEffect;

    [DataField, AutoNetworkedField]
    public EntProtoId WallPrototype;

    [DataField, AutoNetworkedField]
    public TimeSpan BuildTime;

    [DataField, AutoNetworkedField]
    public List<NetCoordinates> TileCenters = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Effects = new();
}
