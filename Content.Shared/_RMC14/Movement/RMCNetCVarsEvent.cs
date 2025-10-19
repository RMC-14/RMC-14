using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Movement;

[Serializable, NetSerializable]
public sealed class RMCNetCVarsEvent(int bufferSize) : EntityEventArgs
{
    public readonly int BufferSize = bufferSize;
}
