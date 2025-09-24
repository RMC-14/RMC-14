using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
}