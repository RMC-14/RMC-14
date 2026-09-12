using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCSurgeryComplicationEffectsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? SuccessBleedDamage;

    [DataField, AutoNetworkedField]
    public int? FailureBleedDamage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? SuccessDirectDamage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? FailureDirectDamage;

    [DataField, AutoNetworkedField]
    public bool SuccessSplashEnabled;

    [DataField, AutoNetworkedField]
    public bool FailureSplashEnabled;

    [DataField, AutoNetworkedField]
    public float SplashRadius = 2.5f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? SplashDamage;

    [DataField, AutoNetworkedField]
    public bool SplashAffectsXenos;

    [DataField, AutoNetworkedField]
    public bool SplashAffectsBody;

    [DataField, AutoNetworkedField]
    public SoundSpecifier SplashSound = new SoundCollectionSpecifier("XenoAcidSizzle");

    [DataField, AutoNetworkedField]
    public EntProtoId? SplashDecalSpawner;
}