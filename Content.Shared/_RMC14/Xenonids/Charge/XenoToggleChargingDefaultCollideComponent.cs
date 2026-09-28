using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoToggleChargingSystem))]
public sealed partial class XenoToggleChargingDefaultCollideComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerStage = new(); // CM13: momentum * 5 (or 8 if max, handled by the mult below)

    [DataField, AutoNetworkedField]
    public float MaxStageDamageMultiplier = 1.6f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier BarricadeDamagePerStage = new();  // CM13: momentum * 22

    [DataField, AutoNetworkedField]
    public DamageSpecifier StructureDamagePerStage = new();  // CM13: momentum * 40

    [DataField, AutoNetworkedField]
    public int StageLoss = 1;

    [DataField, AutoNetworkedField]
    public float KnockbackStrengthPerStage = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxStageStunTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
