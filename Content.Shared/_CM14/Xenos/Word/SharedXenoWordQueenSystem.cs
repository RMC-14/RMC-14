using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._CM14.Xenos.Word;

public abstract class SharedXenoWordQueenSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
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
        if (args.Handled)
            return;

        _ui.TryOpenUi(queen.Owner, XenoWordQueenUI.Key, queen);
    }

    protected virtual void OnXenoWordQueenBui(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenBuiMessage args)
    {
    }

    private void OnChatMaxMessageLengthChanged(int value)
    {
        CharacterLimit = value;
    }
}
