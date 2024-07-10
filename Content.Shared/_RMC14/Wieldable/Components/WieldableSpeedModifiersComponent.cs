using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldableSpeedModifiersComponent : Component
{
    // Generic formula: 1 / (1 / SS14_MOVE_SPEED + SS13_MOVE_DELAY / 10) / SS14_MOVE_SPEED
    // Since dynamically calculating this isn't practical with how changing movespeed works, the formulae below have the default movement speeds already inserted.
    /// <summary>
    /// The base multiplier, which is then altered by attachments and armour movement compensation.
    /// Formula for conversion from 13: 1 / (0.4 + SS13_MOVE_DELAY / 10) / 2.5
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseWalk = 1f;

    [DataField, AutoNetworkedField]
    public float ModifiedWalk = 1f;

    /// <summary>
    /// The base multiplier, which is then altered by attachments and armour movement compensation.
    /// Formula for conversion from 13: 1 / (0.22 + SS13_MOVE_DELAY / 10) / 4.5
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseSprint = 1f;

    [DataField, AutoNetworkedField]
    public float ModifiedSprint = 1f;
}
