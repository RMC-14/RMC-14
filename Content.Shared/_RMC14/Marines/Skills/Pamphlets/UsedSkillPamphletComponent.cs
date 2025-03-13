using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Skills.Pamphlets;

/// <summary>
///     Used to indicate that a marine has already used a skill pamphlet. Can also give a marine a special job title or squad icon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UsedSkillPamphletComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? Icon;

    [DataField, AutoNetworkedField]
    public LocId? JobTitle;
}
