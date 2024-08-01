using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSelectiveFireSystem))]
public sealed partial class RMCSelectiveFireComponent : Component
{
    /// <summary>
    /// The base fire modes available to the weapon. This will override what's set in the weapon's GunComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SelectiveFire BaseFireModes = SelectiveFire.SemiAuto;

    /// <summary>
    /// The base recoil when the weapon is wielded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecoilWielded = 1f;

    /// <summary>
    /// The base recoil when the weapon is not wielded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecoilUnwielded = 1f;

    /// <summary>
    /// Equivalent to GunComponent.AngleIncrease.
    /// This exists to properly reset the angle increase after switching firemodes.
    /// This generally should not be changed. Instead, use ShotsToMaxScatter in the relevant firemode's entry in Modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterIncrease = Angle.FromDegrees(0.0);

    /// <summary>
    /// Equivalent to GunComponent.AngleDecay.
    /// This should not be changed. RMC guns reset their scatter to the minimum instantly after shooting.
    /// This is here to make sure the scatter decay doesn't get overriden by something someone sets in the weapon's GunComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterDecay = Angle.FromDegrees(0.0);

    /// <summary>
    /// Equivalent to GunComponent.MinAngle and GunComponent.MaxAngle
    /// This is the base scatter value for a wielded weapon.
    /// Scatter is the angle of the cone within which your shots deviate from where your cursor is.
    /// Conversion from 13 guns: scatter * 2
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterWielded = Angle.FromDegrees(10.0);

    /// <summary>
    /// Equivalent to GunComponent.MinAngle and GunComponent.MaxAngle
    /// This is the base scatter value for an unwielded weapon.
    /// Scatter is the angle of the cone within which your shots deviate from where your cursor is.
    /// Conversion from 13 guns: scatter_unwielded * 2
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterUnwielded = Angle.FromDegrees(10.0);

    /// <summary>
    /// Equivalent to GunComponent.FireRate.
    /// This is how many shots a weapon fires per second.
    /// Conversion from 13 guns: 1 / (fire_delay / 10)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseFireRate = 1.429f;

    /// <summary>
    /// This is the multiplier applied to the additional scatter added by a SelectiveFireModifierSet with UseBurstScatterMult set to true.
    /// Conversion from 13 guns: burst_scatter_mult
    /// </summary>
    [DataField, AutoNetworkedField]
    public double BurstScatterMult = 4.0;

    /// <summary>
    /// This is the modified burst scatter multiplier. This should not be set manually, it's handled by RMCSelectiveFireSystem.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double BurstScatterMultModified = 4.0;

    /// <summary>
    /// This dictionary contains the modifiers used for different firemodes.
    /// If a firemode isn't in here, it doesn't get any of the modifiers applied to it and will not have variable scatter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<SelectiveFire, SelectiveFireModifierSet> Modifiers = new()
    {
        { SelectiveFire.Burst, new SelectiveFireModifierSet(0.1f, 10.0, true, 2.0, 6) },
        { SelectiveFire.FullAuto, new SelectiveFireModifierSet(0f, 26.0, true, 2.0, 4) }
    };
}

[DataRecord, Serializable, NetSerializable]
public record struct SelectiveFireModifierSet(
    /// <summary>
    /// Additional fire delay applied to the weapon when this mode is active.
    /// A weapon's fire delay is the delay in seconds between each shot. It's inversely proportionate to the weapon's rate of fire.
    /// Conversion from rate of fire: 1 / FireRate
    /// Conversion from 13 guns for burst fire: burst_delay / 10 * 0.666. Due to how burst delay is handled in 13, we need the 0.666.
    /// </summary>
    float FireDelay,

    /// <summary>
    /// A flat modifier applied to the weapon's maximum scatter.
    /// This is multiplied by UnwieldedScatterMultiplier if the weapon is not wielded and by BurstScatterMultModified if UseBurstScatterMult is true.
    /// This modifier will never reduce the weapon's scatter — only increase it or keep it the same — even if made negative by its multipliers.
    /// Conversion from 13 guns for burst fire: 10
    /// Conversion from 13 guns for fully-automatic fire: fa_max_scatter * 2
    /// </summary>
    double MaxScatterModifier,

    /// <summary>
    /// If this is set to true, the additional scatter added by this modifier set will be multiplied by BurstScatterMultModified.
    /// </summary>
    bool UseBurstScatterMult,

    /// <summary>
    /// The additional scatter added by this modifier set will be multiplied by this value if the weapon is not wielded.
    /// </summary>
    double UnwieldedScatterMultiplier,

    /// <summary>
    /// This determines how many shots it takes to reach maximum scatter.
    /// If it's set to null, the weapon will not accumulate scatter when firing.
    /// </summary>
    int? ShotsToMaxScatter
);
