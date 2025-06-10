using Content.Server.Chat.Systems;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.OnCollide;

public sealed class OnCollideSystem : SharedOnCollideSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void DoEmote(Entity<DamageOnCollideComponent> ent, EntityUid other)
    {
        if (ent.Comp.Emote is not { } emote)
            return;


        if (HasComp<XenoComponent>(other) && ent.Comp.XenoEmote != null)
        {
            emote = ent.Comp.XenoEmote.Value;
        }
        else if (HasComp<XenoComponent>(other))
            return;

        _chat.TryEmoteWithChat(other, emote);
    }
}
