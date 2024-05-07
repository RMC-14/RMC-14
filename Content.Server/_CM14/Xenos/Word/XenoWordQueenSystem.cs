using System.Text.RegularExpressions;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared._CM14.Xenos.Word;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Xenos.Word;

public sealed class XenoWordQueenSystem : SharedXenoWordQueenSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private readonly Regex _newLineRegex = new("\n{3,}", RegexOptions.Compiled);

    protected override void OnXenoWordQueenBui(Entity<XenoWordQueenComponent> queen, ref XenoWordQueenBuiMessage args)
    {
        _ui.CloseUi(queen.Owner, XenoWordQueenUI.Key, queen);

        var text = args.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(queen.Owner, queen.Comp.PlasmaCost, false))
            return;

        if (!TryComp(queen, out XenoComponent? queenXeno))
        {
            _popup.PopupEntity("Nobody could hear you...", queen, queen, PopupType.LargeCaution);
            return;
        }

        if (text.Length > CharacterLimit)
            text = text[..CharacterLimit].Trim();

        var xenos = Filter
            .Empty()
            .AddWhereAttachedEntity(ent => CompOrNull<XenoComponent>(ent)?.Hive == queenXeno.Hive);

        if (xenos.Count <= 1)
        {
            _popup.PopupEntity("Nobody could hear you...", queen, queen, PopupType.LargeCaution);
            return;
        }

        _xenoPlasma.TryRemovePlasma(queen.Owner, queen.Comp.PlasmaCost);

        text = _newLineRegex.Replace(text, "\n\n");
        var wrapped = FormattedMessage.EscapeText(text);
        const string header = "[color=#921992][font size=16][bold]The words of the Queen reverberate in our head...[/bold][/font][/color]\n";
        var message = $"{header}[color=red][font size=14][bold]{wrapped}[/bold][/font][/color]";

        _chat.ChatMessageToManyFiltered(xenos, ChatChannel.Radio, text, message, queen, false, true, null);
        _audio.PlayGlobal(queen.Comp.Sound, xenos, true);

        foreach (var (actionId, _) in _actions.GetActions(queen))
        {
            if (HasComp<XenoWordQueenActionComponent>(actionId))
                _actions.StartUseDelay(actionId);
        }
    }
}
