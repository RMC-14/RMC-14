using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Audio;

[Serializable, NetSerializable]
public sealed class RMCAudioPlayGlobalEvent(SoundSpecifier sound, AudioParams audioParams, ushort component) : EntityEventArgs
{
    public readonly SoundSpecifier Sound = sound;
    public readonly AudioParams AudioParams = audioParams;
    public readonly ushort Component = component;
}
