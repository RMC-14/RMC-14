using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoShardSystem))]
public sealed partial class XenoShardComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Shards = 0;

    [DataField, AutoNetworkedField]
    public int MaxShards = 300;

    [DataField, AutoNetworkedField]
    public float ArmorPerShard = 0.05f;

    [DataField, AutoNetworkedField]
    public int ShardsPerArmorBonus = 50;

    [DataField, AutoNetworkedField]
    public float ShardGrowthRate = 5f; // shard_gain_onlife = 5

    [DataField, AutoNetworkedField]
    public int ShardsOnDamage = 15; // shards_per_slash = 15



    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.45f; // shard_lock_speed_mod
}