using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Parasite;
[ByRefEvent]
public record struct VictimInfectedEmoteEvent(ProtoId<EmotePrototype> Emote);
