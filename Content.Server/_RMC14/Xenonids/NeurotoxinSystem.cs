using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Xenonids.Neurotoxin;

namespace Content.Server._RMC14.Xenonids;

public sealed class NeurotoxinSystem : SharedNeurotoxinSystem
{
    [Dependency] ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeurotoxinComponent, NeurotoxinEmoteEvent>(OnEmote);
    }

    public void OnEmote(Entity<NeurotoxinComponent> victim, ref NeurotoxinEmoteEvent args)
    {
        _chat.TryEmoteWithChat(victim, args.Emote);
    }
}
