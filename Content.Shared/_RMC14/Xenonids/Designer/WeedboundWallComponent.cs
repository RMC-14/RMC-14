using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RMC14.Xenonids.Designer;

// Marks a wall or door as bound to weeds.
// When the bound weeds are destroyed, this wall collapses.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeedboundWallComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? BoundNode;

    [DataField, AutoNetworkedField]
    public NetEntity? BoundWeed;

    [DataField, AutoNetworkedField]
    public bool IsThickVariant = false;
}
