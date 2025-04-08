using System.Diagnostics;
using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Squads;

public sealed class SquadTypeParser : TypeParser<SquadType>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override bool TryParse(ParserContext ctx, out SquadType result)
    {
        if (ctx.GetWord(ParserContext.IsToken) is not { } squad)
        {
            ctx.Error = new NotAValidSquad(null);
            result = default;
            return false;
        }

        if (!int.TryParse(squad, out var number))
        {
            ctx.Error = new NotAValidSquad(squad);
            result = default;
            return false;
        }

        ctx.Error = null;
        result = new SquadType(new EntityUid(number));
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? argName)
    {
        var squadsQuery = _entities.EntityQueryEnumerator<SquadTeamComponent, MetaDataComponent>();
        var squads = new List<CompletionOption>();
        while (squadsQuery.MoveNext(out var uid, out _, out var meta))
        {
            squads.Add(new CompletionOption(uid.ToString(), meta.EntityName));
        }

        return CompletionResult.FromHintOptions(squads, "squad");
    }
}

public readonly record struct SquadType(EntityUid Value) : IAsType<EntityUid>
{
    public EntityUid AsType()
    {
        return Value;
    }
}

public record NotAValidSquad(string? Squad) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = Squad == null ? "No squad was given!" : $"{Squad} is not a valid squad entity ID!";
        return FormattedMessage.FromMarkupPermissive(msg);
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
