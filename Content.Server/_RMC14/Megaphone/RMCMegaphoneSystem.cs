using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Megaphone;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<MegaphoneMessageEvent>(OnMegaphoneMessage);
    }

    private void OnMegaphoneMessage(MegaphoneMessageEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        var actor = GetEntity(ev.Actor);
        if (!HasComp<ActorComponent>(actor))
            return;

        _chat.TrySendInGameICMessage(actor, ev.Message, InGameICChatType.Speak, false);
    }
}
