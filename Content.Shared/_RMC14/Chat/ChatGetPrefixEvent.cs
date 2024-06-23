using Content.Shared.Radio;

namespace Content.Shared._RMC14.Chat;

[ByRefEvent]
public record struct ChatGetPrefixEvent(RadioChannelPrototype? Channel);
