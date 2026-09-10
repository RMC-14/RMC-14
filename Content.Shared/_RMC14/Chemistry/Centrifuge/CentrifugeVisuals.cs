using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Centrifuge;

[Serializable, NetSerializable]
public enum CentrifugeVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum CentrifugeVisualState
{
    EmptyOpen,
    EmptyClosed,
    OnOpen,
    OnClosed,
    Spinning,
    Finish,
}
