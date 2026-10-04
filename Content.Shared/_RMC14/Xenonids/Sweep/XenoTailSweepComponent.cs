using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Sweep;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoTailSweepSystem))]
public sealed partial class XenoTailSweepComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 10;

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    //range we add to ability entity checking range. loosely based on the distance a vest marine can travel in 750ms.
    //lagcomp will always be clamped to 750ms anyway, so only downside of larger values is a miniscule performance hit.
    [DataField]
    public float LagCompensationLookupMargin = 4f;

    [DataField, AutoNetworkedField]
    public float KnockBackDistance = 1f;

    [DataField]
    public DamageSpecifier? Damage;

    // TODO RMC14 scale with damage dealt up to a cap
    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField, AutoNetworkedField]
    public EntProtoId HitEffect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg");
}
