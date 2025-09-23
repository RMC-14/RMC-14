using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Icons;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class IconCommand : ToolshedCommand
{
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

    [CommandImplementation("set")]
    public EntityUid Set([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument] string rsiState)
    {
        _marineSystem ??= GetSys<SharedMarineSystem>();

        var icon = new SpriteSpecifier.Rsi(new("_RMC14/Interface/cm_job_icons.rsi"), rsiState);

        // TODO RMC14: Make this idiot proof. Right now it's very easy to cause this command to render a big ol' error on everyone's screen.

        _marineSystem.SetMarineIcon(marine, icon);

        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] string rsiState)
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
}
