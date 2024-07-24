using Content.Server.Chat.Systems;
using Content.Shared._RMC14.OnCollide;

namespace Content.Server._RMC14.OnCollide;

public sealed class OnCollideSystem : SharedOnCollideSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void DoNewCollide(Entity<DamageOnCollideComponent> ent, EntityUid other)
    {
        if (ent.Comp.Emote is { } emote)
            _chat.TryEmoteWithChat(other, emote);
    }
}
