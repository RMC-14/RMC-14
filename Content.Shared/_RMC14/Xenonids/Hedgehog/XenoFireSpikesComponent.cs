using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoShardSystem))]
public sealed partial class XenoFireSpikesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ShardCost = 75;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownExpireAt;

    [DataField, AutoNetworkedField]
    public int SpikeCount = 8;

    [DataField, AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public DamageSpecifier MoveDamage = new();

    [DataField, AutoNetworkedField]
    public EntProtoId Projectile = "XenoHedgehogSpikeProjectile";

    [DataField, AutoNetworkedField]
    public int ProjectileCount = 8;

    [DataField, AutoNetworkedField]
    public int? ProjectileHitLimit = 6;
}
