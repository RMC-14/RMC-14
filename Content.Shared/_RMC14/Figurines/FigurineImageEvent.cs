using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Figurines;

[Serializable, NetSerializable]
public sealed class FigurineImageEvent(byte[] image) : EntityEventArgs
{
    public readonly byte[] Image = image;
}
