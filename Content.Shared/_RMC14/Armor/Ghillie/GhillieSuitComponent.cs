using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Ghillie;

/// <summary>
/// Component for ghillie suit abilities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGhillieSuitSystem))]
public sealed partial class GhillieSuitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;

    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan InvisibilityDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// ID of the use delay for the ghillie suit invisiblity.
    /// </summary>
    [DataField]
    public string DelayId = "rmc_ghillie_suit";
}