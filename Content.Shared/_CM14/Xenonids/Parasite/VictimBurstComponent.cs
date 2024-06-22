using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class VictimBurstComponent : Component
{
    [DataField, AutoNetworkedField]
    public Enum BurstLayer = VictimInfectedLayer.Burst;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier BurstSprite = new Rsi(new ResPath("/Textures/_CM14/Effects/burst.rsi"), "bursted_stand");
}
