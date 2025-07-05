using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

/// <summary>
/// Prevents this entity from being ignited by fires that are not on the bypass whitelist.
/// Unlike RMCImmuneToFireTileDamageComponent, this completely prevents ignition rather than just blocking damage.
/// This is useful for entities that should be completely immune to normal fires.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCImmuneToIgnitionComponent : Component
{
    /// <summary>
    /// Optional whitelist of fire types that can bypass this immunity and ignite the entity.
    /// If null, the entity is immune to all ignition attempts.
    /// If specified, only fires matching this whitelist can ignite the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? BypassWhitelist;
}
