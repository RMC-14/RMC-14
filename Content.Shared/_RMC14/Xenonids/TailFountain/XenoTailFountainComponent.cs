using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.TailFountain;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class XenoTailFountainComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier ExtinguishSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId Acid = "XenoAcidExtinguishEffect";
}
