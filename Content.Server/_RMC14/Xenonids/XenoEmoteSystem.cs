using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids;

public sealed class XenoEmoteSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoComponent, EmoteEvent>(OnXenoEmote);
        SubscribeLocalEvent<XenoComponent, ComponentStartup>(OnXenoStartup);
    }

    private void OnXenoStartup(Entity<XenoComponent> xeno, ref ComponentStartup args)
    {
        if (xeno.Comp.EmoteSounds == null)
            return;

        _proto.TryIndex(xeno.Comp.EmoteSounds, out xeno.Comp.Sounds);
    }

    private void OnXenoEmote(Entity<XenoComponent> xeno, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _chat.TryPlayEmoteSound(xeno, xeno.Comp.Sounds, args.Emote);
    }
}
