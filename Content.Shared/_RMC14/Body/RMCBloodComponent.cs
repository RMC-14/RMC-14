using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBloodComponent : Component
{
    /// <summary>
    /// If false will never regen or do low blood effects, it will still bleed
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProcessBlood = true;

    [DataField, AutoNetworkedField]
    public TimeSpan NextBloodUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan NextBleedingUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan DoBleedEvery = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateBloodEvery = TimeSpan.FromSeconds(2);

    //Max blood is the solutionContainer limit so no need to track
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodNormal = 560;

    //Below safe some effects start to happen
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodSafe = 501;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodOkay = 336;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodBad = 224;

    //Below this mob instant dies
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodSurvive = 112;

    //Blood Regen Stuff

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodRegen = 0.1f;

    [DataField, AutoNetworkedField]
    public float BloodRegenNutritionCost = 0.25f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 OverlimitBloodLoss = 0.1f;

    //Low blood effect stuff

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> OxygenDamage = "Airloss";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> ToxinDamage = "Toxin";

    [DataField, AutoNetworkedField]
    public float VOxylossDivisor = 5;

    [DataField, AutoNetworkedField]
    public float MaxVOxylossDivisor = 2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxVOxyloss = 100;
}
