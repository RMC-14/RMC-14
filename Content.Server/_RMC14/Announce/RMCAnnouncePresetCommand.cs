using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce;

[AdminCommand(AdminFlags.Moderator)]
public sealed class RMCAnnouncePresetCommand : LocalizedEntityCommands
{
    [Dependency] private readonly RMCAnnouncementOverlaySystem _announcementOverlay = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly string[] Targets = ["all", "marines", "xenos"];

    public override string Command => "announcepreset";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_announcementOverlay.TryGetPreset(args[0], out _))
        {
            shell.WriteError(Loc.GetString("cmd-announcepreset-invalid-preset", ("preset", args[0])));
            return;
        }

        var target = args.Length == 3 ? args[2].ToLowerInvariant() : "all";
        var filter = target switch
        {
            "all" => Filter.Broadcast(),
            "marines" => Filter.Empty().AddWhereAttachedEntity(e => EntityManager.HasComponent<MarineComponent>(e) || EntityManager.HasComponent<GhostComponent>(e)),
            "xenos" => Filter.Empty().AddWhereAttachedEntity(e => EntityManager.HasComponent<XenoComponent>(e) || EntityManager.HasComponent<GhostComponent>(e)),
            _ => null,
        };

        if (filter == null)
        {
            shell.WriteError(Loc.GetString("cmd-announcepreset-invalid-target", ("target", args[2])));
            return;
        }

        _announcementOverlay.Show(new RMCAnnouncementOverlay(args[0], args[1], shell.Player?.AttachedEntity), filter);
        shell.WriteLine(Loc.GetString("cmd-announcepreset-sent", ("count", filter.Count)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<EntityPrototype>()
                    .Where(p => p.TryGetComponent(out RMCAnnouncementPresetComponent? _, _compFactory))
                    .Select(p => p.ID),
                Loc.GetString("cmd-announcepreset-hint-preset")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-announcepreset-hint-message")),
            3 => CompletionResult.FromHintOptions(Targets, Loc.GetString("cmd-announcepreset-hint-target")),
            _ => CompletionResult.Empty,
        };
    }
}
