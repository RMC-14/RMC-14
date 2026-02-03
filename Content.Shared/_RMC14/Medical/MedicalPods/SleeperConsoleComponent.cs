using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.MedicalPods;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperConsoleComponent : Component
{
    /// <summary>
    /// The linked sleeper pod entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedSleeper;

    /// <summary>
    /// Skills required to operate the console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SkillWhitelist? SkillRequired;
}
