using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class VictimBurstComponent : Component
{
    [DataField, AutoNetworkedField]
    public ResPath RsiPath = new("/Textures/_RMC14/Effects/burst.rsi");

    [DataField, AutoNetworkedField]
    public string BurstState = "bursted_stand";

    [DataField, AutoNetworkedField]
    public string BurstingState = "burst_stand";
}

[Serializable, NetSerializable]
public enum VictimBurstState : byte
{
    Bursting = 1,
    Burst = 2
}
