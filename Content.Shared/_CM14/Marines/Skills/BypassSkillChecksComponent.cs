using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SkillsSystem))]
public sealed partial class BypassSkillChecksComponent : Component;
