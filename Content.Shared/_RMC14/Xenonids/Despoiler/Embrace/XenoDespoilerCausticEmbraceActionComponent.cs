using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Embrace;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerCausticEmbraceActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NormalRange = 1;

    [DataField, AutoNetworkedField]
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

    [DataField, AutoNetworkedField]
    public float LingeringAcidChance = 0.3f;

    [DataField, AutoNetworkedField]
    public TimeSpan EmpoweredWeakenDuration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float SplashScanSize = 3f;

    [DataField, AutoNetworkedField]
    public EntProtoId<TimedDespawnComponent> TelegraphProto = "RMCEffectDespoilerCausticTelegraph";

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerLingeringAcidComponent> LingeringAcidProto = "RMCEffectDespoilerLingeringAcid";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? PounceSound;
}
