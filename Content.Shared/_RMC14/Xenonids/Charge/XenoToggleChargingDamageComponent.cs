using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

// TODO RMC14: vending machines, electrified fences, disposal unit collision, bed, filing cabinet, fuel tanks, prison windows, mounted machineguns, power loaders,
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoToggleChargingDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public int StageLoss;

    [DataField, AutoNetworkedField]
    public float StageLossProbability = 1;

    [DataField, AutoNetworkedField]
    public bool Destroy;

    [DataField, AutoNetworkedField]
    public bool Stop;

    [DataField, AutoNetworkedField]
    public bool Unanchor;

    [DataField, AutoNetworkedField]
    public int MinimumStage = 1;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PercentageDamage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? ArmorPiercingDamage;

    [DataField, AutoNetworkedField]
    public int ArmorPiercing;

    // TODO RMC14 damageable in shared
    [DataField, AutoNetworkedField]
    [Access(typeof(SharedRMCDamageableSystem))]
    public FixedPoint2 DestroyDamage;

    [DataField, AutoNetworkedField]
    public int DefaultMultiplier;

    [DataField, AutoNetworkedField]
    public Dictionary<int, int>? StageMultipliers;
}
