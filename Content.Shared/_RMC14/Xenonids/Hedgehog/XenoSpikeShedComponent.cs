using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoShardSystem), typeof(XenoSpikeShieldSystem))]
public sealed partial class XenoSpikeShedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MinShards = 50;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ShardLockDuration = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan ShardLockExpireAt;

    [DataField, AutoNetworkedField]
    public float SpeedBoost = 1.5f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier ShardDamage = new();

    [DataField, AutoNetworkedField]
    public float ShedRadius = 4f;

    [DataField, AutoNetworkedField]
    public EntProtoId Projectile = "XenoHedgehogSpikeProjectile";

    [DataField, AutoNetworkedField]
    public int ProjectileCount = 40;

    [DataField, AutoNetworkedField]
    public int? ProjectileHitLimit = 6;
}
