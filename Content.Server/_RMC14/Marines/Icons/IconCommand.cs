using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Icons;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class IconCommand : ToolshedCommand
{
    private static readonly ResPath JobIconsRoot = new("/Textures/_RMC14/Interface/job_icons");

    [Dependency] private readonly IResourceManager _resource = default!;

    private SharedMarineSystem? _marineSystem;

    [CommandImplementation("get_human_readable")]
    public string GetHumanReadable([PipedArgument] EntityUid marine)
    {
        var icon = EntityManager.GetComponentOrNull<MarineComponent>(marine)?.Icon;

        switch (icon)
        {
            case SpriteSpecifier.Texture t:
                return "Texture path: " + t.TexturePath.CanonPath;
            case SpriteSpecifier.Rsi r:
                return "RSI: " + r.RsiPath.Filename + "+" + r.RsiState;
            case SpriteSpecifier.EntityPrototype e:
                return "EntityPrototype: " + e.EntityPrototypeId;
            case null:
                return "No icon.";
            default:  // This case should, in theory, never be hit. The above four cases cover all options.
                return "Something is very wrong here.";
        }
    }

    [CommandImplementation("get")]
    public SpriteSpecifier? Get([PipedArgument] EntityUid marine)
    {
        return EntityManager.GetComponentOrNull<MarineComponent>(marine)?.Icon;
    }

    [CommandImplementation("set")] //TODO: Maybe a verb & UI for this so we can see what the icons look like.
    public EntityUid Set([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument(typeof(MarineIconStateParser))] string rsiState)
    {
        _marineSystem ??= GetSys<SharedMarineSystem>();

        var icon = TryFindIcon(rsiState);
        if (icon == null)
        {
            ctx.WriteLine($"No .rsi under {JobIconsRoot} contains a state named '{rsiState}'.");
            return marine;
        }

        _marineSystem.SetMarineIcon(marine, icon);

        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument(typeof(MarineIconStateParser))] string rsiState)
    {
        return marines.Select(marine => Set(ctx, marine, rsiState));
    }

    [CommandImplementation("del")]
    public EntityUid Del([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        _marineSystem ??= GetSys<SharedMarineSystem>();

        _marineSystem.ClearMarineIcon(marine);

        return marine;
    }

    [CommandImplementation("del")]
    public IEnumerable<EntityUid> Del([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => Del(ctx, marine));
    }

    private SpriteSpecifier.Rsi? TryFindIcon(string rsiState)
    {
        foreach (var (rsi, state) in EnumerateStates(_resource))
        {
            if (state == rsiState)
                return new SpriteSpecifier.Rsi(rsi, state);
        }

        return null;
    }

    // reads from all Meta.jsons under the JobIconsRoot
    private static IEnumerable<(ResPath Rsi, string State)> EnumerateStates(IResourceManager resource)
    {
        foreach (var meta in resource.ContentFindFiles(JobIconsRoot))
        {
            if (meta.Filename != "meta.json")
                continue;

            if (!resource.TryContentFileRead(meta, out var stream))
                continue;

            using (stream)
            {
                using var doc = JsonDocument.Parse(stream);
                if (!doc.RootElement.TryGetProperty("states", out var states))
                    continue;

                foreach (var element in states.EnumerateArray())
                {
                    if (element.TryGetProperty("name", out var name) && name.GetString() is { } n)
                        yield return (meta.Directory, n);
                }
            }
        }
    }

    // TAB completion of job icon names for icon:set NO " " NEEDED!!
    public sealed class MarineIconStateParser : CustomTypeParser<string>
    {
        [Dependency] private readonly IResourceManager _resource = default!;

        public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out string? result)
        {
            result = ctx.GetWord(ParserContext.IsToken);
            if (result is null)
            {
                ctx.Error = new OutOfInputError();
                return false;
            }

            return true;
        }

        public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
        {
            var states = EnumerateStates(_resource)
                .Select(x => x.State)
                .Distinct()
                .OrderBy(x => x);

            return CompletionResult.FromHintOptions(states, GetArgHint(arg));
        }
    }
}
