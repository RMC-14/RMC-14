using Discord.Interactions;

namespace Content.DiscordBot.Modules;

public class LinkAccountModal : IModal
{
    public string Title => "Link SS14 account";

    [InputLabel("SS14 Account Name")]
    [ModalTextInput("account_name")]
    public string AccountName { get; set; } = string.Empty;
}
