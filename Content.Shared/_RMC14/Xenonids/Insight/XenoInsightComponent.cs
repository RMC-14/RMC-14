using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Insight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoInsightSystem))]
public sealed partial class XenoInsightComponent : Component
{
    //min stacks
    [DataField, AutoNetworkedField]
    public int Insight = 0;

    //max stacks
    [DataField, AutoNetworkedField]
    public int MaxInsight = 10;
}
