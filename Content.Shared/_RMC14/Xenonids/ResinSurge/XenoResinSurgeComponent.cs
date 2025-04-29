using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoResinSurgeSystem))]
public sealed partial class XenoResinSurgeComponent : Component
{
    // Amount of time to deduct from fruit growth timer
    [DataField, AutoNetworkedField]
    public TimeSpan FruitGrowth = TimeSpan.FromSeconds(5);

    // Amount of hitpoints to reinforce structure by
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReinforceAmount = 6000;

    // Amount of time to reinforce structure for
    [DataField, AutoNetworkedField]
    public TimeSpan ReinforceDuration = TimeSpan.FromSeconds(15);

    // TODO: Color of reinforced door/wall if possible

    // Length of do-after for surging sticky resin
    [DataField, AutoNetworkedField]
    public TimeSpan StickyResinDoAfterPeriod = TimeSpan.FromSeconds(1);

    // Radius of sticky resin to create (0 - 1x1 tile, 1 - 3x3 tiles, 2 - 5x5 tiles etc)
    [DataField, AutoNetworkedField]
    public int StickyResinRadius = 1;

    [DataField]
    public DoAfterId? ResinDoafter;

    // Prototype for unstable wall to create
    [DataField, AutoNetworkedField]
    public EntProtoId UnstableWallId = "WallXenoResinWeak";

    // Prototype for resin to create
    [DataField, AutoNetworkedField]
    public EntProtoId StickyResinId = "XenoStickyResinWeak";

    [DataField, AutoNetworkedField]
    public int Range = 7;
}
