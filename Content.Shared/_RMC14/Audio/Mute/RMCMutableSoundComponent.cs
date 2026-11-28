using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Audio.Mute;

[RegisterComponent]
[Access(typeof(RMCMutedSoundsSystem))]
public sealed partial class RMCMutableSoundComponent : Component
{
    [DataField]
    public List<ProtoId<SoundCollectionPrototype>> Collections = new();
}
