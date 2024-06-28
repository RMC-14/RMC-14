using Discord.Interactions;

namespace Content.DiscordBot.Modules;

public class LinkAccountModal : IModal
{
    public string Title => "Link SS14 account";

    [InputLabel("SS14 Linking Code (top left in the lobby)")]
    [RequiredInput]
    [ModalTextInput("account_code")]
    public string Code { get; set; } = string.Empty;
}
