using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Acid;

[Serializable, NetSerializable]
public sealed partial class ServerCorrodingEvent : EntityEventArgs
{
    [DataField]
    public float ExpendableLightDps = 2.5f;

    public ServerCorrodingEvent(float expendableLightDps)
    {
        ExpendableLightDps = expendableLightDps;
    }
}
