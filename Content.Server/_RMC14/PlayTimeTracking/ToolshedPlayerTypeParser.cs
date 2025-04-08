using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.PlayTimeTracking;

public sealed class ToolshedPlayerTypeParser : TypeParser<ToolshedPlayer>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ToolshedPlayer? result)
    {
        if (ctx.GetWord(c => ParserContext.IsToken(c) || c == new Rune('@')) is not { } name)
        {
            ctx.Error = new NotAValidPlayer(null);
            result = null;
            return false;
        }

        ctx.Error = null;
        result = new ToolshedPlayer(name);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, "Player");
    }
}

public record NotAValidPlayer(string? Player) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = Player == null ? "No player was given<!" : $"{Player} is not a valid player!";
        return FormattedMessage.FromMarkupPermissive(msg);
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record ToolshedPlayer(string Name)
{
    public override string ToString()
    {
        return Name;
    }
}
