using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    public virtual void Announce(EntityUid sender, string message, ProtoId<RadioChannelPrototype> channel)
    {
    }
}
