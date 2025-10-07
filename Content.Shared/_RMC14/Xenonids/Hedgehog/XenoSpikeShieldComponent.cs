using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoShardSystem), typeof(XenoSpikeShieldSystem))]
public sealed partial class XenoSpikeShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ShieldDuration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan ShieldExpireAt;

    [DataField, AutoNetworkedField]
    public int ShardCost = 150;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(11);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownExpireAt;

    [DataField, AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new();

    [DataField, AutoNetworkedField]
    public float SpikeRadius = 1.5f; // 3x3 AoE

    [DataField, AutoNetworkedField]
    public bool Active = false;

    [DataField, AutoNetworkedField]
    public TimeSpan LastProcTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public float AccumulatedDamage;

    [DataField, AutoNetworkedField]
    public EntProtoId Projectile = "XenoHedgehogShieldSpikeProjectile";

    [DataField, AutoNetworkedField]
    public int ProjectileCount = 9;

    [DataField, AutoNetworkedField]
    public int? ProjectileHitLimit = 6;
}
