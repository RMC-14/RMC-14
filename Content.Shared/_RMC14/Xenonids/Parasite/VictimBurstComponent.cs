using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class VictimBurstComponent : Component
{
    [DataField, AutoNetworkedField]
    public BurstVisualState State = BurstVisualState.Bursting;

    [DataField, AutoNetworkedField]
    public Enum Layer = BurstLayer.Base;

    [DataField, AutoNetworkedField]
    public ResPath BurstPath = new("/Textures/_RMC14/Effects/burst.rsi");

    [DataField, AutoNetworkedField]
    public string BurstState = "bursted_stand";

    [DataField, AutoNetworkedField]
    public string BurstingState = "burst_stand";
}
