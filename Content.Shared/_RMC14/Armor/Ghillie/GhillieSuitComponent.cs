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
    public float Opacity = 0.01f;

    /// <summary>
    /// How desaturated the entity should become while invisible.
    /// 0 = no desaturation, 1 = fully grayscale.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GreyFactor;

    /// <summary>
    /// How dark the entity should become while invisible.
    /// 0 = no change, 1 = every color becomes black
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DarkFactor;

    /// <summary>
    /// How much opacity is added whenever the user fires a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AddedOpacityOnShoot = 0.04f;

    /// <summary>
    /// How long the do-after of the ability takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long it takes for the cloak the cloak to fade out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InvisibilityDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long it takes for the cloak to start fading again if it is broken. (For example, shooting a gun)
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InvisibilityBreakDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public bool BlockFriendlyFire = true;

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public EntProtoId CloakEffect = "RMCEffectCloak";

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionGhilliePreparePosition";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
