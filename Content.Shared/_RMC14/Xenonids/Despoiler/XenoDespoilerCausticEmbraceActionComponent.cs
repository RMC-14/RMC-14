using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent]
public sealed partial class XenoDespoilerCausticEmbraceActionComponent : Component
{
    [DataField]
    public int NormalRange = 1;

    [DataField]
    public int EmpoweredRange = 5;

    [DataField]
    public DamageSpecifier SplashDamage = new()
    {
        DamageDict = { ["Heat"] = 30 },
    };

    [DataField]
    public DamageSpecifier EmpoweredDamage = new()
    {
        DamageDict = { ["Heat"] = 30 },
    };

    [DataField]
    public float LingeringAcidChance = 0.3f;

    [DataField]
    public TimeSpan EmpoweredWeakenDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float SplashScanSize = 3f;

    [DataField]
    public EntProtoId TelegraphProto = "RMCEffectDespoilerCausticTelegraph";

    [DataField]
    public EntProtoId LingeringAcidProto = "RMCEffectDespoilerLingeringAcid";

    [DataField]
    public SoundSpecifier? PounceSound;
}
