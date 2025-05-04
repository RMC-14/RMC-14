using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public float Range = 8;

    [DataField, AutoNetworkedField]
    public float SlowRange = 1.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(3.5);

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan ChargeDelay = TimeSpan.FromSeconds(1.2);

    // TODO RMC14 extra sound on impact
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg");

    [DataField, AutoNetworkedField]
    public Vector2? Charge;

    [DataField, AutoNetworkedField]
    public float Strength = 20;
}
