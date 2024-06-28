using Discord.Interactions;

namespace Content.DiscordBot.Modules;

public class LinkAccountModal : IModal
{
    public string Title => "Link SS14 account";

    [InputLabel("SS14 Linking Code (Get this in-game in the lobby)")]
    [RequiredInput]
    [ModalTextInput("code", maxLength: 100)]
    public string Code { get; set; } = string.Empty;
}
