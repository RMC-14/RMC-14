using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCAirShotComponent : Component
{
    /// <summary>
    ///     If it's possible to shoot the gun into the air if there is a roof.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreRoof;

    /// <summary>
    ///     How long it takes to complete the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PreparationTime = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     If the user needs to be in combat mode to shoot into the air.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresCombat;

    /// <summary>
    ///     How many shakes
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShakeAmount;

    /// <summary>
    ///     The strength of every shake
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShakeStrength = 30;

    /// <summary>
    ///     The skills needed to shoot the gun into the air.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int>? RequiredSkills;

    /// <summary>
    ///     The identifier of the last shot signal flare.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LastFlareId;
}
