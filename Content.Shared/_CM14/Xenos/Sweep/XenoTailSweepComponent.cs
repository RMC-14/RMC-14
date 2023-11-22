using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Sweep;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoTailSweepSystem))]
public sealed partial class XenoTailSweepComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaCost = 10;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 1;

    [DataField]
    public DamageSpecifier? Damage;

    // TODO CM14 scale with damage dealt up to a cap
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId HitEffect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_CM14/Xeno/alien_claw_block.ogg");
}
