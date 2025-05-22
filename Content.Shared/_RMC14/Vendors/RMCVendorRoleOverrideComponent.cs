using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVendorRoleOverrideComponent : Component
{
    /// <summary>
    /// New title for the marine. If IsAppendSquadRoleName is true, it will be appended to the current role name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? GiveSquadRoleName;

    /// <summary>
    /// If true, RoleName will be appended to the current role name. If false - replaces the current role name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsAppendSquadRoleName;

    /// <summary>
    /// New icon for the marine in the interface.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? GiveIcon;
}