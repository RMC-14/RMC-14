using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._RMC14.SkillsCommand;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class SkillsCommand : ToolshedCommand
{
    private SkillsSystem? _skills;

    [CommandImplementation("get")]
    public int Get([PipedArgument] EntityUid marine, [CommandArgument] SkillType skill)
    {
        var skillComp = EntityManager.GetComponentOrNull<SkillsComponent>(marine);
        if (skillComp == null)
            return 0;

        var skills = skillComp.Skills;
        return (int) (typeof(Skills).GetProperty(skill.Value)?.GetValue(skills) ?? 0);
    }

    [CommandImplementation("set")]
    public EntityUid Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument] SkillType skill,
        [CommandArgument] ValueRef<int> level)
    {
        _skills ??= GetSys<SkillsSystem>();
        var skillsComp = EnsureComp<SkillsComponent>(marine);
        object skills = skillsComp.Skills;
        var levelValue = level.Evaluate(ctx);
        typeof(Skills).GetProperty(skill.Value)?.SetValue(skills, levelValue);

        _skills.SetSkills((marine, skillsComp), (Skills) skills);
        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] SkillType skill,
        [CommandArgument] ValueRef<int> level)
    {
        return marines.Select(marine => Set(ctx, marine, skill, level));
    }
}
