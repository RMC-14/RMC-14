using System.Reflection;
using Content.DiscordBot.Modules;
using Content.Server.Database;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot;

public sealed class CommandHandler(DiscordSocketClient client, CommandService commands, InteractionService interaction, PostgresServerDbContext db)
{
    public async Task InstallCommandsAsync()
    {
        client.MessageReceived += HandleCommandAsync;
        client.ButtonExecuted += HandleButtonAsync;
        client.ModalSubmitted += HandleModalAsync;

        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        interaction.AddModalInfo<LinkAccountModal>();
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Don't process the command if it was a system message
        var message = messageParam as SocketUserMessage;
        if (message == null)
            return;

        // Create a number to track where the prefix ends and the command begins
        var argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
            return;

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(client, message);

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        await commands.ExecuteAsync(context, argPos, null);
    }

    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        switch (component.Data.CustomId)
        {
            case "link-ss14-account":
                await component.RespondWithModalAsync<LinkAccountModal>("link-ss14-account");
                break;
        }
    }

    private async Task HandleModalAsync(SocketModal modal)
    {
        switch (modal.Data.CustomId)
        {
            case "link-ss14-account":
                var name = modal.Data.Components.First(c => c.CustomId == "account_name").Value;
                if (string.IsNullOrWhiteSpace(name))
                    break;

                var player = await db.Player.FirstOrDefaultAsync(p => p.LastSeenUserName.ToLower() == name.ToLower());
                if (player == null)
                    break;

                var author = modal.Message.Author.Id;
                var account = db.RMCLinkedAccounts.FirstOrDefault(l => l.DiscordId == author);
                if (account == null)
                {
                    account = new RMCLinkedAccount
                    {
                        PlayerId = player.UserId,
                        DiscordId = author,
                    };

                    db.RMCLinkedAccounts.Add(account);
                }

                await db.SaveChangesAsync();
                break;
        }
    }
}
