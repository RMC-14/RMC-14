using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Damage;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Damage;

public sealed class RMCDamageableSystem : SharedRMCDamageableSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void DoEmote(EntityUid ent, ProtoId<EmotePrototype> emote)
    {
        _chat.TryEmoteWithoutChat(ent, emote);
    }
}
