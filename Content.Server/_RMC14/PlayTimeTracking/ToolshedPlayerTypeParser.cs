using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.PlayTimeTracking;

public sealed class ToolshedPlayerTypeParser : TypeParser<ToolshedPlayer>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        if (parserContext.GetWord(c => ParserContext.IsToken(c) || c == new Rune('@')) is not { } name)
        {
            error = new NotAValidPlayer(null);
            result = null;
            return false;
        }

        error = null;
        result = new ToolshedPlayer(name);
        return true;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return (CompletionResult.FromHintOptions(options, "Player"), null);
    }
}

public record NotAValidPlayer(string? Player) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = Player == null ? "No player was given!" : $"{Player} is not a valid player!";
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
