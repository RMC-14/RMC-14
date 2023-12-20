using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoConstructionComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 BuildRange = 1;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> CanBuild = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? BuildChoice;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BuildDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 OrderConstructionRange = 1.5;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> CanOrderConstruction = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityCoordinates? OrderingConstructionAt;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OrderConstructionDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OrderConstructionAddPlasmaDelay = TimeSpan.FromSeconds(3);

}
