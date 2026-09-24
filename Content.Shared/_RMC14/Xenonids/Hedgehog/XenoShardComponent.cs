using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoShardSystem))]
public sealed partial class XenoShardComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ArmorPerShard = 2.5f;

    [DataField, AutoNetworkedField]
    public int ShardsPerArmorBonus = 50;

    [DataField, AutoNetworkedField]
    public int ShardsOnDamage = 10; // shards gained when hit by projectiles

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.45f; // shard_lock_speed_mod

    [DataField, AutoNetworkedField]
    public TimeSpan? SpikeShedCooldownEnd;
}
