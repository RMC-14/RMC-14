using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem))]
public sealed partial class CMArmorComponent : Component
{
    // TODO RMC14 other types of armor
    [DataField, AutoNetworkedField]
    public int Armor;

    // TODO RMC14 some rockets should penetrate armor
    // TODO RMC14 tank/sniper flak/shotgun incendiary burst is resisted by this but penetrated
    [DataField, AutoNetworkedField]
    public int ExplosionArmor;

    // The generic conversion formula from move delay to additive multiplier is as follows: 1 / (1 / SS14_SPEED + SS13_MOVE_DELAY) / SS14_SPEED - 1
    // The formulae below already have the default human speeds inserted. Use them when converting from SS13.
    /// <summary>
    /// This is the amount by which the additive speed multiplier from wielded items is changed. This one applies to walking speed.
    /// Converting to an additive multiplier from SS13 move delay: 1 / (0.4 + SS13_MOVE_DELAY) / 2.5 - 1
    /// Since this is supposed to increase speed, take the negative of the result.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WieldSlowdownCompensationWalk = 0f;

    /// <summary>
    /// This is the amount by which the additive speed multiplier from wielded items is changed. This one applies to sprinting speed.
    /// Converting to an additive multiplier from SS13 move delay: 1 / (0.22 + SS13_MOVE_DELAY) / 4.5 - 1
    /// Since this is supposed to increase speed, take the negative of the result.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WieldSlowdownCompensationSprint = 0f;
}
