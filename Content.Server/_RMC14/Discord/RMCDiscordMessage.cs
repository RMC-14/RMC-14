namespace Content.Server._RMC14.Discord;

public readonly record struct RMCDiscordMessage(string Author, string Message, RMCDiscordMessageType Type);
