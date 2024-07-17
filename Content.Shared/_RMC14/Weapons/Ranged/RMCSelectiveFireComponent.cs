using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSelectiveFireSystem))]
public sealed partial class RMCSelectiveFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RecoilWielded = 1f;

    [DataField, AutoNetworkedField]
    public float RecoilUnwielded = 1f;

    [DataField, AutoNetworkedField]
    public Angle BaseAngleIncrease = Angle.FromDegrees(5);

    [DataField, AutoNetworkedField]
    public Angle BaseAngleDecay = Angle.FromDegrees(0.0);

    [DataField, AutoNetworkedField]
    public Angle ScatterWielded = Angle.FromDegrees(10.0);

    [DataField, AutoNetworkedField]
    public Angle ScatterUnwielded = Angle.FromDegrees(10.0);

    [DataField, AutoNetworkedField]
    public float BaseFireRate = 1.43f;

    [DataField, AutoNetworkedField]
    public double BurstScatterMult = 4.0;

    [DataField, AutoNetworkedField]
    public double BurstScatterMultModified = 4.0;

    [DataField, AutoNetworkedField]
    public Dictionary<SelectiveFire, SelectiveFireModifierSet> Modifiers = new()
    {
        { SelectiveFire.Burst, new SelectiveFireModifierSet(-0.1f, 10.0, true, 2.0, 6) },
        { SelectiveFire.FullAuto, new SelectiveFireModifierSet(0f, 26.0, true, 2.0, 4) }
    };
}

[DataRecord, Serializable, NetSerializable]
public record struct SelectiveFireModifierSet(
    float FireDelay, // Conversion from CM for burst: burst_delay / 10 * 0.666
    double MaxScatterModifier,
    bool UseBurstScatterMult,
    double UnwieldedScatterMultiplier,
    int? ShotsToMaxScatter
);
