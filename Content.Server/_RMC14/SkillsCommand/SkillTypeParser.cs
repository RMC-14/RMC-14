using System.Diagnostics;
using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.SkillsCommand;

public sealed class SkillTypeParser : TypeParser<SkillType>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override bool TryParse(ParserContext ctx, out SkillType result)
    {
        if (ctx.GetWord(ParserContext.IsToken) is not { } skillName)
        {
            ctx.Error = new NotAValidSkill(null);
            result = default;
            return false;
        }

        var skills = _entities.System<SkillsSystem>().SkillNames;
        if (!skills.TryGetValue(skillName, out var skill))
        {
            ctx.Error = new NotAValidSkill(skillName);
            result = default;
            return false;
        }

        ctx.Error = null;
        result = new SkillType(skill);
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? argName)
    {
        var skills = _entities.System<SkillsSystem>().SkillNames.Keys.Order(StringComparer.OrdinalIgnoreCase);
        return CompletionResult.FromHintOptions(skills, "skill");
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
