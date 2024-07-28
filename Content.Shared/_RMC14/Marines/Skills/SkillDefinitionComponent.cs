using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SkillsSystem))]
public sealed partial class SkillDefinitionComponent : Component;
