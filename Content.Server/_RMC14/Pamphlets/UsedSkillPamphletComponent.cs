using Robust.Shared.GameStates;

namespace Content.Server._RMC14.Pamphlets;

/// <summary>
///     Used to indicate that a marine has already used a skill pamphlet
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UsedSkillPamphletComponent : Component;
