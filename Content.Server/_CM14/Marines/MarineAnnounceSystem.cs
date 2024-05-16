using Content.Server.Radio.EntitySystems;
using Content.Shared._CM14.Marines.Announce;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Announce(EntityUid sender, string message, ProtoId<RadioChannelPrototype> channel)
    {
        base.Announce(sender, message, channel);

        _radio.SendRadioMessage(sender, message, channel, sender);
    }
}
