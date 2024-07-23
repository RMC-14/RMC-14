using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveConstructionNodeComponent : Component
{
    /// <summary>
    /// How much plasma it costs to place this template.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 InitialPlasmaCost = 400;

    /// <summary>
    /// How much plasma needs to be stored to complete the construction.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 PlasmaCost;

    /// <summary>
    /// How much plasma has been stored so far.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaStored;

    /// <summary>
    /// The entity to spawn once enough plasma is stored.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Spawn;

    [DataField, AutoNetworkedField]
    public bool BlockOtherNodes = true;
}
