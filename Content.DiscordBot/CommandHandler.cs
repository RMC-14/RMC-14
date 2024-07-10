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
                if (modal.GuildId is not { } guildId)
                    break;

                var codeStr = modal.Data.Components.First(c => c.CustomId == "account_code").Value.Trim();
                if (string.IsNullOrWhiteSpace(codeStr))
                    break;

                await modal.DeferAsync(true);
                if (!Guid.TryParse(codeStr, out var code))
                {
                    await modal.FollowupAsync($"{codeStr} isn't a valid code! Get one in-game from the lobby at the top left of the screen.", ephemeral: true);
                }

                var author = modal.User;
                var authorId = author.Id;
                var discord = await db.RMCDiscordAccounts
                    .Include(d => d.LinkedAccount)
                    .ThenInclude(l => l.Player)
                    .ThenInclude(p => p.Patron)
                    .FirstOrDefaultAsync(a => a.Id == authorId);
                var codes = await db.RMCLinkingCodes
                    .Include(l => l.Player)
                    .ThenInclude(player => player.Patron)
                    .FirstOrDefaultAsync(p => p.Code == code);

                if (codes == null)
                {
                    await modal.FollowupAsync($"No player found with code {codeStr}, join the game server and get another code before trying again, or ask for help in another channel.", ephemeral: true);
                    break;
                }

                if (codes.CreationTime < DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
                {
                    await modal.FollowupAsync($"Code {codeStr} were generated too long ago, join the game server and get another code before trying again.", ephemeral: true);
                }

                if (discord?.LinkedAccount is { } linked)
                {
                    if (linked.Player.Patron is { } patron)
                        db.RMCPatrons.Remove(patron);

                    linked.Player.Patron = null;
                    db.RMCLinkedAccounts.Remove(linked);
                }

                discord ??= db.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = authorId }).Entity;
                discord.LinkedAccount = db.RMCLinkedAccounts.Add(new RMCLinkedAccount { Discord = discord }).Entity;
                discord.LinkedAccount.Player = codes.Player;

                var roles = client.GetGuild(guildId).GetUser(authorId).Roles.Select(r => r.Id).ToArray();
                var tiers = await db.RMCPatronTiers
                    .Where(t => roles.Contains(t.DiscordRole))
                    .ToListAsync();
                if (tiers.Count == 0)
                {
                    discord.LinkedAccount.Player.Patron = null;
                }
                else
                {
                    tiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    var tier = tiers[0];
                    discord.LinkedAccount.Player.Patron = db.RMCPatrons.Add(new RMCPatron { Tier = tier }).Entity;
                    discord.LinkedAccount.Player.Patron.Tier = tier;
                }

                db.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
                {
                    Discord = discord,
                    Player = discord.LinkedAccount.Player,
                });

                db.ChangeTracker.DetectChanges();
                await db.SaveChangesAsync();

                var msg = $"Linked SS14 account with name {codes.Player.LastSeenUserName}";
                if (codes.Player.Patron != null)
                    msg += $" and tier {codes.Player.Patron.Tier.Name}";

                await modal.FollowupAsync(msg, ephemeral: true);
                break;
        }
    }
}
