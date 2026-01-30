using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCImmuneToFireTileDamageComponent : Component
{
    /// <summary>
    /// A whitelist of fire types that can bypass this immunity.
    /// If not specified, the entity is immune to all fire tile damage.
    /// If specified, only fires NOT on this whitelist will be blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? BypassWhitelist;
}
