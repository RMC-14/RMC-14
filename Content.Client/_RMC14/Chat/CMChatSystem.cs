using System.Linq;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chat;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chat;

public sealed class CMChatSystem : SharedCMChatSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private int _repeatHistory;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, RMCCVars.RMCChatRepeatHistory, v => _repeatHistory = v, true);
    }

    public bool TryRepetition(
        ChatBox chat,
        OutputPanel contents,
        FormattedMessage message,
        NetEntity sender,
        string unwrapped,
        ChatChannel channel,
        bool repeatCheckSender,
        string? languageIcon)
    {
        var repeated = false;
        foreach (var old in chat.RepeatQueue)
        {
            if (!old.Message.Equals(unwrapped) ||
                old.Channel != channel ||
                old.LanguageIcon != languageIcon)
            {
                continue;
            }

            if (repeatCheckSender &&
                !old.SenderEntity.Equals(sender))
            {
                continue;
            }

            old.Count++;
            var copy = new FormattedMessage(old.FormattedMessage);
            copy.AddMarkupPermissive($" [color=red]x{old.Count}[/color]");
            old.IconControl?.Orphan();
            contents.SetMessage(old.Index, copy);
            if (old.LanguageIcon != null)
                old.IconControl = contents.Children.OfType<LanguageIconTag.LanguageIconControl>().LastOrDefault();
            repeated = true;
            break;
        }

        if (!repeated)
        {
            chat.RepeatQueue.Enqueue(new RepeatedMessage(contents.EntryCount, message, sender, unwrapped, channel, languageIcon));
            if (_repeatHistory > 0)
            {
                while (chat.RepeatQueue.Count > _repeatHistory)
                {
                    chat.RepeatQueue.Dequeue();
                }
            }
        }

        return repeated;
    }
}
