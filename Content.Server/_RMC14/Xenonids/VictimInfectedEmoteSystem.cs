using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Xenonids.Parasite;

namespace Content.Shared._RMC14.Xenonids;

public sealed class VictimInfectedEmoteSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VictimInfectedComponent, VictimInfectedEmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<VictimInfectedComponent> ent, ref VictimInfectedEmoteEvent args)
    {
        _chat.TryEmoteWithChat(ent, args.Emote);
    }
}
