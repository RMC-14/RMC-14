using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._RMC14.Announce.Commands;


[AdminCommand(AdminFlags.Moderator)]
public sealed class AnnouncePresetCommand : IConsoleCommand
{
    public string Command => "announcepreset";
    public string Description => "Send an announcement using any configured preset.";
    public string Help => "announcepreset <presetId> \"<message>\" [--entity=<uid>] [--name=\"<speaker name>\"] [--target=All|Marines|Xenos]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var announceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GeneralAnnounceSystem>();

        if (args.Length < 2)
        {
            shell.WriteError("Usage: announce <presetId> \"<message>\" [--entity=<uid>] [--name=\"<speaker name>\"] [--target=All|Marines|Xenos]");
            return;
        }

        var presetId = args[0];
        var (message, options) = ParseMessageAndOptions(args.Skip(1).ToArray());

        if (string.IsNullOrWhiteSpace(message))
        {
            shell.WriteError("Message cannot be empty");
            return;
        }

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId
        };

        if (options.TryGetValue("entity", out var entityStr) &&
            EntityUid.TryParse(entityStr, out var entityId))
        {
            request.Speaker = entityId;
        }
        else if (shell.Player?.AttachedEntity is { } playerEntity)
        {
            request.Speaker = playerEntity;
        }

        if (options.TryGetValue("name", out var name))
        {
            request.SpeakerNameOverride = name;
        }

        if (options.TryGetValue("target", out var targetStr) &&
            Enum.TryParse<AnnouncementTarget>(targetStr, true, out var target))
        {
            request.Target = target;
        }

        announceSystem.AnnounceAdvanced(request);
        shell.WriteLine($"Sent announcement preset '{presetId}': {message}");
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
