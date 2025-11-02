using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHandLabelerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int LabelsLeft = 50;

    [DataField, AutoNetworkedField]
    public int MaxLabels = 50;

    [DataField]
    public SoundSpecifier LabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/component_pickup.ogg", AudioParams.Default.WithMaxDistance(0));

    [DataField]
    public SoundSpecifier RemoveLabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/paper_ripped.ogg", AudioParams.Default.WithMaxDistance(0));

    [DataField]
    public EntityUid? CurrentPillBottle;
}
