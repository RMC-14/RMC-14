using Content.Shared._RMC14.Maths;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoChargeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 20;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public float Range = RMCMathExtensions.CircleAreaFromSquareAbilityRange(7);

    [DataField, AutoNetworkedField]
    public float SlowRange = 1.5f;

    [DataField, AutoNetworkedField]
    public float KnockBackDistance = 3f;

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(3.5);

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan ChargeDelay = TimeSpan.FromSeconds(1.2);

    // TODO RMC14 extra sound on impact
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg");

    [DataField]
    public DamageSpecifier VehicleDamage = new() { DamageDict = new() { { "Blunt", 100 } } };

    [DataField, AutoNetworkedField]
    public Vector2? Charge;

    [DataField, AutoNetworkedField]
    public float Strength = 20;

    /// <summary>
    /// How much plasma to refund when hitting something that's not blacklisted
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HitRefundPlasma = 10;

    /// <summary>
    /// How much to reduce cooldown when hitting something that's not blacklisted
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HitRefundCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Blacklist for entities that don't give a refund on hit
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? HitRefundBlacklist;

    [DataField]
    public HashSet<EntityUid> AlreadyHit = new();

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";
}
