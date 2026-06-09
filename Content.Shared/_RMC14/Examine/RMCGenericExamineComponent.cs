using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Examine;

/// <summary>
///    A generic component for giving an entity examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMExamineSystem))]
public sealed partial class RMCGenericExamineComponent : Component
{
    /// <summary>
    ///    The Loc ID of the examine text.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId MessageId;

    /// <summary>
    ///    The priority of the examine text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ExaminePriority = 0;

    /// <summary>
    ///    The skills required to view the examine text. Optional field.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SkillWhitelist? SkillsRequired;

    /// <summary>
    ///    Entities that are on the blacklist will not see the examine text. Optional field.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///    Only entities that are on the whitelist will see the examine text. Optional field.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
