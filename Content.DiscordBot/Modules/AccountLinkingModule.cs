using Discord;
using Discord.Commands;

namespace Content.DiscordBot.Modules;

public sealed class AccountLinkingModule : ModuleBase<SocketCommandContext>
{
    [Command("create")]
    [RequireOwner]
    public Task CreateAsync()
    {
        var component = new ComponentBuilder()
            .WithButton("Link your SS14 account here!", "link-ss14-account")
            .Build();

        return ReplyAsync(string.Empty, components: component);
    }
}
