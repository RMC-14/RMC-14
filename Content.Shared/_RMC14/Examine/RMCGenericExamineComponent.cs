using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Examine;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Examine;

/// <summary>
///    A generic component for giving an entity examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGenericExamineComponent : Component
{
    /// <summary>
    ///    The examine text. Supports localization.
    /// </summary>
    /// <remarks>
    ///    PLEASE use localization keys whenever possible.
    /// </remarks>
    [DataField(required: true), AutoNetworkedField]
    public string Message;

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

    /// <summary>
    ///    How the examine text should be displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCExamineDisplayMode DisplayMode = RMCExamineDisplayMode.Direct;

    /// <summary>
    ///    Configuration for detailed examine verb. Only used if DisplayMode is DetailedVerb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCDetailedVerbConfig? DetailedVerbConfig;
}

[Serializable, NetSerializable]
public enum RMCExamineDisplayMode
{
    /// <summary>
    ///    Shows the examine text directly in the examine window.
    /// </summary>
    Direct,

    /// <summary>
    ///    Shows the examine text in a detailed examine verb (button with icon).
    /// </summary>
    DetailedVerb
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCDetailedVerbConfig
{
    /// <summary>
    ///    The icon texture path for the detailed examine verb button.
    /// </summary>
    [DataField]
    public string VerbIcon = ExamineSystemShared.DefaultIconTexture;

    /// <summary>
    ///    The message shown when hovering over the detailed examine verb button.
    /// </summary>
    [DataField]
    public LocId HoverMessageId = string.Empty;

    /// <summary>
    ///    The title shown in the detailed examine verb panel header.
    /// </summary>
    [DataField(required: true)]
    public LocId Title;
}
