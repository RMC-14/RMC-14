using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Marines.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MedicallyUnskilledDoAfterComponent, AttemptHyposprayUseEvent>(OnAttemptHyposprayUse);
        SubscribeLocalEvent<RequiresSkillComponent, BeforeRangedInteractEvent>(OnRequiresSkillBeforeRangedInteract);
        SubscribeLocalEvent<ReagentExaminationRequiresSkillComponent, ExaminedEvent>(OnExamineReagentContainer);
    }

    private void OnAttemptHyposprayUse(Entity<MedicallyUnskilledDoAfterComponent> ent, ref AttemptHyposprayUseEvent args)
    {
        if (!TryComp(args.User, out SkillsComponent? skills) ||
            skills.Skills.Medical < ent.Comp.Min)
        {
            args.MaxDoAfter(ent.Comp.DoAfter);
        }
    }

    private void OnRequiresSkillBeforeRangedInteract(Entity<RequiresSkillComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!HasSkills(args.User, in ent.Comp.Skills))
        {
            _popup.PopupClient($"You don't know how to use the {Name(args.Used)}...", args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnExamineReagentContainer(Entity<ReagentExaminationRequiresSkillComponent> ent, ref ExaminedEvent args)
    {
        if (!HasSkills(args.Examiner, in ent.Comp.Skills))
            return;

        if (!TryComp(args.Examined, out SolutionContainerManagerComponent? solutionContainerManager) || solutionContainerManager is null)
        {
            return;
        }
        var foundReagents = new HashSet<string>();
        foreach (var solutionContainerID in solutionContainerManager.Containers)
        {
            if (!_solutionContainerSystem.TryGetSolution(args.Examined, solutionContainerID, out _, out var solution) || solution is null)
            {
                continue;
            }
            foreach (var reagent in solution.Contents)
            {
                var reagentName = _prototypeManager.Index<ReagentPrototype>(reagent.Reagent.Prototype).LocalizedName;
                foundReagents.Add(reagentName);
            }
        }
        if (!foundReagents.Any())
        {
            args.PushMarkup(Loc.GetString("reagents-examine-action-found-none"));
            return;
        }

        int reagentCount = foundReagents.Count;
        int i = 0;
        var fullMessage = "";
        fullMessage += Loc.GetString("reagents-examine-action-found");
        fullMessage += " ";
        foreach (var reagentId in foundReagents)
        {
            fullMessage += reagentId;
            if (i > reagentCount)
            {
                fullMessage += ", ";
            }
            ++i;
        }
        args.PushMarkup(fullMessage);
    }

    public TimeSpan GetDelay(EntityUid user, EntityUid tool)
    {
        if (!TryComp(tool, out MedicallyUnskilledDoAfterComponent? doAfter) ||
            doAfter.Min <= 0)
        {
            return default;
        }

        if (!TryComp(user, out SkillsComponent? skills) ||
            skills.Skills.Medical < doAfter.Min)
        {
            return doAfter.DoAfter;
        }

        return default;
    }

    public bool HasSkills(Entity<SkillsComponent?> ent, in Skills required)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        // TODO CM14 turn these into prototypes
        Resolve(ent, ref ent.Comp, false);
        var skills = ent.Comp?.Skills ?? default;
        return skills.Antagonist >= required.Antagonist &&
               skills.Construction >= required.Construction &&
               skills.Cqc >= required.Cqc &&
               skills.Domestics >= required.Domestics &&
               skills.Endurance >= required.Endurance &&
               skills.Engineer >= required.Engineer &&
               skills.Execution >= required.Execution &&
               skills.Firearms >= required.Firearms &&
               skills.Fireman >= required.Fireman &&
               skills.Intel >= required.Intel &&
               skills.Jtac >= required.Jtac &&
               skills.Leadership >= required.Leadership &&
               skills.Medical >= required.Medical &&
               skills.MeleeWeapons >= required.MeleeWeapons &&
               skills.Navigations >= required.Navigations &&
               skills.Overwatch >= required.Overwatch &&
               skills.Pilot >= required.Pilot &&
               skills.Police >= required.Police &&
               skills.PowerLoader >= required.PowerLoader &&
               skills.Research >= required.Research &&
               skills.Smartgun >= required.Smartgun &&
               skills.SpecialistWeapons >= required.SpecialistWeapons &&
               skills.Surgery >= required.Surgery &&
               skills.Vehicles >= required.Vehicles;
    }

    public void SetSkills(Entity<SkillsComponent> ent, in Skills skills)
    {
        ent.Comp.Skills = skills;
        Dirty(ent);
    }
}
