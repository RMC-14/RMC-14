using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RMC14.Xenonids.Designer;

// Marks a wall or door as bound to weeds.
// When the bound weeds are destroyed, this wall collapses.
[Access(typeof(DesignerConstructNodeSystem), typeof(WeedboundWallSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeedboundWallComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? BoundNode;

    [DataField, AutoNetworkedField]
    public NetEntity? BoundWeed;

    [DataField, AutoNetworkedField]
    public EntProtoId ResiduePrototype = "XenoStickyResinWeak";

    [DataField, AutoNetworkedField]
    public EntProtoId ThickResiduePrototype = "XenoStickyResin";

    [DataField]
    public EntityUid? BoundWeedUid;

    [DataField, AutoNetworkedField]
    public bool IsThickVariant = false;
}
