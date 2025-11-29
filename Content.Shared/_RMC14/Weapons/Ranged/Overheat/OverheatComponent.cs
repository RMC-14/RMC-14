using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Overheat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverheatComponent : Component
{
    /// <summary>
    ///     Current heat level of the weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Heat;

    /// <summary>
    ///     Maximum heat before the weapon overheats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxHeat = 40;

    /// <summary>
    ///     The amount of heat gained per shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatPerShot = 1;

    /// <summary>
    ///     The amount of heat lost per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CooldownRate = 2;

    /// <summary>
    ///     The value the current heat is multiplied by when the weapon overheats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EmergencyCooldownMultiplier = 0.375f;

    /// <summary>
    ///     The amount of seconds the weapon won't be able to shoot after overheating.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EmergencyCooldownDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The damage dealt to the weapon(or it's mount) when it overheats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new ()
    {
        DamageDict =
        {
            ["Heat"] = 30,
        },
    };

    /// <summary>
    ///     Whether the weapon is currently overheated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OverHeated;

    /// <summary>
    ///     Whether the weapon is currently overheated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan OverHeatedAt;

    /// <summary>
    ///     The sound that is played when the weapon overheats
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? OverheatSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/hmg_cooling.ogg");
}
