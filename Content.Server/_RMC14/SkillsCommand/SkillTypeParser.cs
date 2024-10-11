using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.SkillsCommand;

public sealed class SkillTypeParser : TypeParser<SkillType>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        if (parserContext.GetWord(ParserContext.IsToken) is not { } skillName)
        {
            error = new NotAValidSkill(null);
            result = null;
            return false;
        }

        var skills = _entities.System<SkillsSystem>().SkillNames;
        if (!skills.TryGetValue(skillName, out var skill))
        {
            error = new NotAValidSkill(skillName);
            result = null;
            return false;
        }

        error = null;
        result = new SkillType(skill);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        var skills = _entities.System<SkillsSystem>().SkillNames.Keys.Order(StringComparer.OrdinalIgnoreCase);
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHintOptions(skills, "skill"), null));
    }
}

public readonly record struct SkillType(string Value) : IAsType<string>
{
    public string AsType()
    {
        return Value;
    }
}

public record NotAValidSkill(string? Skill) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = Skill == null ? "No skill was given!" : $"{Skill} is not a valid skill!";
        return FormattedMessage.FromMarkupPermissive(msg);
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
