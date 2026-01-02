using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stealth;

/// <summary>
///     Will slowly lower an entity's opacity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPassiveStealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinOpacity = 0.2f;

    [DataField, AutoNetworkedField]
    public float MaxOpacity = 1f;

    [DataField, AutoNetworkedField]
    public bool? Enabled = null;

    /// <summary>
    ///     How long it will take to get to full invisibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.Zero;

    /// <summary>
    ///     How long it will take to get out of invisibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UnCloakDelay = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleTime;

    [DataField]
    public bool Toggleable;

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
