using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class SkillDefinitionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float[] DelayMultipliers = Array.Empty<float>();
}
