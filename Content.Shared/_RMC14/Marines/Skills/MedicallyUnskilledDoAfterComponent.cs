using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class MedicallyUnskilledDoAfterComponent : Component
{
    // TODO RMC14 use Skills struct and IncludeDataField
    [DataField, AutoNetworkedField]
    public int Min = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(3);
}
