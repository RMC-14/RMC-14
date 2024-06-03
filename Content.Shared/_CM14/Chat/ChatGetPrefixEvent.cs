using Content.Shared.Radio;

namespace Content.Shared._CM14.Chat;

[ByRefEvent]
public record struct ChatGetPrefixEvent(RadioChannelPrototype? Channel);
