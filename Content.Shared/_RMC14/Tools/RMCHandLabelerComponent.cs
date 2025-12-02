using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHandLabelerComponent : Component
{
    [DataField]
    public SoundSpecifier LabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/component_pickup.ogg");

    [DataField]
    public SoundSpecifier RemoveLabelSound = new SoundPathSpecifier("/Audio/_RMC14/Items/paper_ripped.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentPillBottle;
}
