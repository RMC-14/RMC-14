using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._CM14.Xenos.Word;

public abstract class SharedXenoWordQueenSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    protected int CharacterLimit = 1000;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoWordQueenComponent, XenoWordQueenActionEvent>(OnXenoWordQueenAction);

        Subs.BuiEvents<XenoWordQueenComponent>(XenoWordQueenUI.Key, sub =>
        {
            sub.Event<XenoWordQueenBuiMessage>(OnXenoWordQueenBui);
        });

        _config.OnValueChanged(CCVars.ChatMaxMessageLength, OnChatMaxMessageLengthChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _config.UnsubValueChanged(CCVars.ChatMaxMessageLength, OnChatMaxMessageLengthChanged);
    }

    private void OnXenoWordQueenAction(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenActionEvent args)
    {
        if (args.Handled || _net.IsClient || !TryComp(queen, out ActorComponent? actor))
            return;

        _ui.TryOpen(queen.Owner, XenoWordQueenUI.Key, actor.PlayerSession);
    }

    protected virtual void OnXenoWordQueenBui(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenBuiMessage args)
    {
    }

    private void OnChatMaxMessageLengthChanged(int value)
    {
        CharacterLimit = value;
    }
}
