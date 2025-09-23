using Content.Server.Administration;
using Content.Server._RMC14.Announce.Core;
using Content.Shared.Administration;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._RMC14.Announce.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class CommandAnnounceCommand : IConsoleCommand
{
    public string Command => "command";
    public string Description => "Send marine high command announcement";
    public string Help => "command \"<message>\" [--entity=<id>] [--name=\"<name>\"] - Send marine command announcement";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: command \"<message>\" [--entity=<id>] [--name=\"<name>\"]");
            return;
        }

        var (message, options) = ParseMessageAndOptions(args);

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        var builder = AnnouncementBuilder.Create(announceSystem)
            .WithPreset("MarineCommand")
            .WithTarget(AnnouncementTarget.Marines)
            .WithMessage(message);

        if (options.TryGetValue("entity", out var entityStr) &&
            EntityUid.TryParse(entityStr, out var entityId))
        {
            builder.WithSpeaker(entityId);
        }
        else if (shell.Player?.AttachedEntity is { } playerEntity)
        {
            builder.WithSpeaker(playerEntity);
        }

        if (options.TryGetValue("name", out var name))
        {
            builder.WithSpeakerNameOverride(name);
        }

        builder.Send();
        shell.WriteLine($"Sent marine command announcement: {message}");
    }

    private (string message, Dictionary<string, string> options) ParseMessageAndOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        var messageParts = new List<string>();
        var inQuotes = false;
        var currentMessage = "";

        foreach (var arg in args)
        {
            if (arg.StartsWith("--") && !inQuotes)
            {
                var optionPart = arg[2..];
                var equalIndex = optionPart.IndexOf('=');

                if (equalIndex > 0)
                {
                    var key = optionPart[..equalIndex];
                    var value = optionPart[(equalIndex + 1)..];
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
                    {
                        value = value[1..^1];
                    }
                    options[key] = value;
                }
            }
            else
            {
                if (arg.StartsWith("\""))
                {
                    inQuotes = true;
                    currentMessage = arg[1..];
                    if (arg.EndsWith("\"") && arg.Length > 1)
                    {
                        inQuotes = false;
                        currentMessage = currentMessage[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                }
                else if (inQuotes)
                {
                    if (arg.EndsWith("\""))
                    {
                        inQuotes = false;
                        currentMessage += " " + arg[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                    else
                    {
                        currentMessage += " " + arg;
                    }
                }
                else if (!inQuotes && messageParts.Count == 0)
                {
                    messageParts.Add(arg);
                }
            }
        }

        if (inQuotes && !string.IsNullOrEmpty(currentMessage))
        {
            messageParts.Add(currentMessage);
        }

        return (string.Join(" ", messageParts), options);
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class CRTAnnounceCommand : IConsoleCommand
{
    public string Command => "crtannounce";
    public string Description => "Send announcement with CRT visual effects";
    public string Help => "crtannounce \"<message>\" <entityId> [preset] - Presets: AresTerminal, RetroTerminal, ModernTerminal, CleanTerminal";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();
        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (args.Length < 2)
        {
            shell.WriteError("Usage: crtannounce \"<message>\" <entityId> [preset]");
            return;
        }

        var message = args[0].Trim('"');
        if (!EntityUid.TryParse(args[1], out var entityId) || !entityManager.EntityExists(entityId))
        {
            shell.WriteError($"Invalid entity ID: {args[1]}");
            return;
        }

        var preset = args.Length > 2 ? args[2] : "AresTerminal";

        announceSystem.AnnounceCRT(entityId, message, preset);

        shell.WriteLine($"Sent CRT announcement ({preset}) with sprite {entityId}: {message}");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AresAnnounceCommand : IConsoleCommand
{
    public string Command => "ares";
    public string Description => "Send ARES AI announcement";
    public string Help => "ares \"<message>\" [--entity=<id>] - Send ARES announcement";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: ares \"<message>\" [--entity=<id>]");
            return;
        }

        var (message, options) = ParseMessageAndOptions(args);

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        var builder = AnnouncementBuilder.Create(announceSystem)
            .WithPreset("Ares")
            .WithTarget(AnnouncementTarget.All)
            .WithMessage(message)
            .WithSpeakerNameOverride("A.R.E.S.");

        if (options.TryGetValue("entity", out var entityStr) &&
            EntityUid.TryParse(entityStr, out var entityId))
        {
            builder.WithSpeaker(entityId);
        }

        builder.Send();
        shell.WriteLine($"Sent ARES announcement: {message}");
    }

    private (string message, Dictionary<string, string> options) ParseMessageAndOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        var messageParts = new List<string>();
        var inQuotes = false;
        var currentMessage = "";

        foreach (var arg in args)
        {
            if (arg.StartsWith("--") && !inQuotes)
            {
                var optionPart = arg[2..];
                var equalIndex = optionPart.IndexOf('=');

                if (equalIndex > 0)
                {
                    var key = optionPart[..equalIndex];
                    var value = optionPart[(equalIndex + 1)..];
                    options[key] = value;
                }
            }
            else
            {
                if (arg.StartsWith("\""))
                {
                    inQuotes = true;
                    currentMessage = arg[1..];
                    if (arg.EndsWith("\"") && arg.Length > 1)
                    {
                        inQuotes = false;
                        currentMessage = currentMessage[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                }
                else if (inQuotes)
                {
                    if (arg.EndsWith("\""))
                    {
                        inQuotes = false;
                        currentMessage += " " + arg[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                    else
                    {
                        currentMessage += " " + arg;
                    }
                }
                else if (!inQuotes && messageParts.Count == 0)
                {
                    messageParts.Add(arg);
                }
            }
        }

        if (inQuotes && !string.IsNullOrEmpty(currentMessage))
        {
            messageParts.Add(currentMessage);
        }

        return (string.Join(" ", messageParts), options);
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class CriticalAnnounceCommand : IConsoleCommand
{
    public string Command => "critical";
    public string Description => "Send critical emergency announcement";
    public string Help => "critical \"<message>\" - Send critical priority announcement to all players";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: critical \"<message>\"");
            return;
        }

        var message = string.Join(" ", args);
        if (message.StartsWith("\"") && message.EndsWith("\"") && message.Length > 1)
        {
            message = message[1..^1];
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        AnnouncementBuilder.Create(announceSystem)
            .WithPreset("Critical")
            .WithTarget(AnnouncementTarget.All)
            .WithMessage(message)
            .Send();

        shell.WriteLine($"Sent critical announcement: {message}");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class EmergencyAnnounceCommand : IConsoleCommand
{
    public string Command => "emergency";
    public string Description => "Send emergency announcement";
    public string Help => "emergency \"<message>\" - Send emergency announcement to all players";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: emergency \"<message>\"");
            return;
        }

        var message = string.Join(" ", args);
        if (message.StartsWith("\"") && message.EndsWith("\"") && message.Length > 1)
        {
            message = message[1..^1];
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        AnnouncementBuilder.Create(announceSystem)
            .WithPreset("Emergency")
            .WithTarget(AnnouncementTarget.All)
            .WithMessage(message)
            .Send();

        shell.WriteLine($"Sent emergency announcement: {message}");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class SquadAnnounceCommand : IConsoleCommand
{
    public string Command => "squad";
    public string Description => "Send squad announcement to marines";
    public string Help => "squad \"<message>\" [--entity=<id>] [--name=\"<n>\"] - Send squad announcement";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: squad \"<message>\" [--entity=<id>] [--name=\"<n>\"]");
            return;
        }

        var (message, options) = ParseMessageAndOptions(args);

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        var builder = AnnouncementBuilder.Create(announceSystem)
            .WithPreset("Squad")
            .WithTarget(AnnouncementTarget.Marines)
            .WithMessage(message);

        if (options.TryGetValue("entity", out var entityStr) &&
            EntityUid.TryParse(entityStr, out var entityId))
        {
            builder.WithSpeaker(entityId);
        }

        if (options.TryGetValue("name", out var name))
        {
            builder.WithSpeakerNameOverride(name);
        }

        builder.Send();
        shell.WriteLine($"Sent squad announcement: {message}");
    }

    private (string message, Dictionary<string, string> options) ParseMessageAndOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        var messageParts = new List<string>();
        var inQuotes = false;
        var currentMessage = "";

        foreach (var arg in args)
        {
            if (arg.StartsWith("--") && !inQuotes)
            {
                var optionPart = arg[2..];
                var equalIndex = optionPart.IndexOf('=');

                if (equalIndex > 0)
                {
                    var key = optionPart[..equalIndex];
                    var value = optionPart[(equalIndex + 1)..];
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
                    {
                        value = value[1..^1];
                    }
                    options[key] = value;
                }
            }
            else
            {
                if (arg.StartsWith("\""))
                {
                    inQuotes = true;
                    currentMessage = arg[1..];
                    if (arg.EndsWith("\"") && arg.Length > 1)
                    {
                        inQuotes = false;
                        currentMessage = currentMessage[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                }
                else if (inQuotes)
                {
                    if (arg.EndsWith("\""))
                    {
                        inQuotes = false;
                        currentMessage += " " + arg[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                    else
                    {
                        currentMessage += " " + arg;
                    }
                }
                else if (!inQuotes && messageParts.Count == 0)
                {
                    messageParts.Add(arg);
                }
            }
        }

        if (inQuotes && !string.IsNullOrEmpty(currentMessage))
        {
            messageParts.Add(currentMessage);
        }

        return (string.Join(" ", messageParts), options);
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class HiveAnnounceCommand : IConsoleCommand
{
    public string Command => "hive";
    public string Description => "Send xenomorph hive mind announcement";
    public string Help => "hive \"<message>\" [--entity=<id>] - Send hive mind announcement to xenos";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Usage: hive \"<message>\" [--entity=<id>]");
            return;
        }

        var (message, options) = ParseMessageAndOptions(args);

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        var builder = AnnouncementBuilder.Create(announceSystem)
            .WithPreset("XenoHive")
            .WithTarget(AnnouncementTarget.Xenos)
            .WithMessage(message)
            .WithSpeakerNameOverride("Hive Mind");

        if (options.TryGetValue("entity", out var entityStr) &&
            EntityUid.TryParse(entityStr, out var entityId))
        {
            builder.WithSpeaker(entityId);
        }

        builder.Send();
        shell.WriteLine($"Sent hive mind announcement: {message}");
    }

    private (string message, Dictionary<string, string> options) ParseMessageAndOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        var messageParts = new List<string>();
        var inQuotes = false;
        var currentMessage = "";

        foreach (var arg in args)
        {
            if (arg.StartsWith("--") && !inQuotes)
            {
                var optionPart = arg[2..];
                var equalIndex = optionPart.IndexOf('=');

                if (equalIndex > 0)
                {
                    var key = optionPart[..equalIndex];
                    var value = optionPart[(equalIndex + 1)..];
                    options[key] = value;
                }
            }
            else
            {
                if (arg.StartsWith("\""))
                {
                    inQuotes = true;
                    currentMessage = arg[1..];
                    if (arg.EndsWith("\"") && arg.Length > 1)
                    {
                        inQuotes = false;
                        currentMessage = currentMessage[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                }
                else if (inQuotes)
                {
                    if (arg.EndsWith("\""))
                    {
                        inQuotes = false;
                        currentMessage += " " + arg[..^1];
                        messageParts.Add(currentMessage);
                        currentMessage = "";
                    }
                    else
                    {
                        currentMessage += " " + arg;
                    }
                }
                else if (!inQuotes && messageParts.Count == 0)
                {
                    messageParts.Add(arg);
                }
            }
        }

        if (inQuotes && !string.IsNullOrEmpty(currentMessage))
        {
            messageParts.Add(currentMessage);
        }

        return (string.Join(" ", messageParts), options);
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class TestCRTEffectsCommand : IConsoleCommand
{
    public string Command => "testcrteffects";
    public string Description => "Test CRT announcement effects with different presets";
    public string Help => "testcrteffects [entityId] [preset] - Test CRT effects. Presets: AresTerminal, RetroTerminal, ModernTerminal, CleanTerminal";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();
        var entityManager = IoCManager.Resolve<IEntityManager>();

        EntityUid? speakerEntity = null;
        var presetId = "AresTerminal";

        if (args.Length > 0 && EntityUid.TryParse(args[0], out var entityId) && entityManager.EntityExists(entityId))
        {
            speakerEntity = entityId;
        }

        if (args.Length > 1)
        {
            presetId = args[1];
        }

        var (message, speakerName) = presetId switch
        {
            "AresTerminal" => ("A.R.E.S. TERMINAL INTERFACE\nCRT Display Mode: ACTIVE\nScanning for hostile signatures...\nTERMINAL_READY>", "A.R.E.S."),
            "RetroTerminal" => (">>> RETRO TERMINAL ACTIVATED <<<\nCRT_MODE: ENABLED\nSCANLINE_EFFECT: ON\nREADY FOR INPUT_", "SYSTEM"),
            "ModernTerminal" => ("MODERN INTERFACE v2.1\nCRT Enhancement: ACTIVE\nSystem Status: ONLINE\nReady for commands...", "SYSTEM"),
            "CleanTerminal" => ("Terminal Interface\nStatus: Ready\nInput: _", "TERMINAL"),
            _ => ("TERMINAL INTERFACE\nStatus: ONLINE\nReady for input...", "SYSTEM")
        };

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId,
            Target = AnnouncementTarget.All,
            Speaker = speakerEntity,
            SpeakerNameOverride = speakerName
        };

        announceSystem.AnnounceAdvanced(request);
        shell.WriteLine($"Sent CRT announcement with preset '{presetId}': {message.Split('\n')[0]}...");
    }
}
