using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHandLabelerComponent : Component
{
    /// <summary>
    /// Number of labels remaining. Set to -1 for infinite labels.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int LabelsLeft = 50;

    [DataField, AutoNetworkedField]
    public int MaxLabels = 50;

    /// <summary>
    /// Whether the hand labeler is currently turned on.
    /// When off, it can only remove labels, not apply them.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Mode;

    [DataField]
    public SoundSpecifier? LabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/component_pickup.ogg");

    [DataField]
    public SoundSpecifier? RemoveLabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/paper_ripped.ogg");
}
