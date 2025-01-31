using System.Text.RegularExpressions;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Word;

public sealed class XenoWordQueenSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCMChatSystem _cmChat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private readonly Regex _newLineRegex = new("\n{3,}", RegexOptions.Compiled);
    private int _characterLimit = 1000;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoWordQueenComponent, XenoWordQueenActionEvent>(OnXenoWordQueenAction);

        Subs.BuiEvents<XenoWordQueenComponent>(XenoWordQueenUI.Key, subs =>
        {
            subs.Event<XenoWordQueenBuiMsg>(OnXenoWordQueenBui);
        });

        Subs.CVar(_config, CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    private void OnXenoWordQueenAction(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _ui.TryOpenUi(queen.Owner, XenoWordQueenUI.Key, queen);
    }

    private void OnXenoWordQueenBui(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenBuiMsg args)
    {
        _ui.CloseUi(queen.Owner, XenoWordQueenUI.Key, queen);

        var text = args.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(queen.Owner, queen.Comp.PlasmaCost))
            return;

        if (_hive.GetHive(queen.Owner) is not {} hive)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-words-of-the-queen-nobody-hear-you"), queen, queen, PopupType.LargeCaution);
            return;
        }

        if (_net.IsClient)
            return;

        if (text.Length > _characterLimit)
            text = text[.._characterLimit].Trim();

        var xenos = Filter
            .Empty()
            .AddWhereAttachedEntity(ent => _hive.IsMember(ent, hive));

        if (xenos.Count <= 1)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-words-of-the-queen-nobody-hear-you"), queen, queen, PopupType.LargeCaution);
            return;
        }

        _xenoPlasma.TryRemovePlasma(queen.Owner, queen.Comp.PlasmaCost);

        text = _newLineRegex.Replace(text, "\n\n");
        text = _cmChat.SanitizeMessageReplaceWords(queen, text);
        var headerText = Loc.GetString("rmc-xeno-words-of-the-queen-header");
        var wrapped = FormattedMessage.EscapeText(text);
        var header = $"{_xenoAnnounce.WrapHive(headerText)}";
        var message = $"{header}[color=red][font size=14][bold]{wrapped}[/bold][/font][/color]";

        _xenoAnnounce.Announce(queen, xenos, text, message, queen.Comp.Sound);

        foreach (var (actionId, _) in _actions.GetActions(queen))
        {
            if (HasComp<XenoWordQueenActionComponent>(actionId))
                _actions.StartUseDelay(actionId);
        }
    }
}
