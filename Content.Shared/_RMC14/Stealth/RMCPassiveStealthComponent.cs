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

    [DataField]
    public bool Enabled;

    [DataField]
    public bool Toggleable;

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
