using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.GameStates;

[Serializable, NetSerializable]
public sealed class RMCSetPredictionEvent(bool enable) : EntityEventArgs
{
    public readonly bool Enable = enable;
}
