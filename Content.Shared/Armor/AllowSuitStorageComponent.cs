using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Armor;

/// <summary>
///     Used on outerclothing to allow use of suit storage
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AllowSuitStorageComponent : Component
{
    /// <summary>
    /// Whitelist for what entities are allowed in the suit storage slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] {"Item"}
    };
}
