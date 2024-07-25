using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MedicallyUnskilledDoAfterComponent, AttemptHyposprayUseEvent>(OnAttemptHyposprayUse);
        SubscribeLocalEvent<RequiresSkillComponent, BeforeRangedInteractEvent>(OnRequiresSkillBeforeRangedInteract);
        SubscribeLocalEvent<MeleeRequiresSkillComponent, AttemptMeleeEvent>(OnRequiresSkillAttemptMelee);
        SubscribeLocalEvent<MeleeRequiresSkillComponent, ThrowItemAttemptEvent>(OnRequiresSkillThrowAttempt);
        SubscribeLocalEvent<MeleeRequiresSkillComponent, UseInHandEvent>(OnRequiresSkillUseInHand, before: [typeof(SharedFlashSystem)]);
        SubscribeLocalEvent<ReagentExaminationRequiresSkillComponent, ExaminedEvent>(OnExamineReagentContainer);
        SubscribeLocalEvent<ExamineRequiresSkillComponent, ExaminedEvent>(OnExamineRequiresSkill);
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
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", args.Used));
            _popup.PopupClient(msg, args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnRequiresSkillAttemptMelee(Entity<MeleeRequiresSkillComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!HasSkills(args.User, in ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
            args.Cancelled = true;
        }
    }

    private void OnRequiresSkillThrowAttempt(Entity<MeleeRequiresSkillComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (!HasSkills(args.User, in ent.Comp.Skills))
        {
            if (_net.IsServer)
            {
                var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
                _popup.PopupEntity(msg, args.User, args.User, PopupType.SmallCaution);
            }

            args.Cancelled = true;
        }
    }

    private void OnRequiresSkillUseInHand(Entity<MeleeRequiresSkillComponent> ent, ref UseInHandEvent args)
    {
        if (!HasSkills(args.User, in ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnExamineReagentContainer(Entity<ReagentExaminationRequiresSkillComponent> ent, ref ExaminedEvent args)
    {
        if (!HasSkills(args.Examiner, in ent.Comp.Skills))
        {
            if (ent.Comp.UnskilledExamine != null)
            {
                using (args.PushGroup(nameof(ReagentExaminationRequiresSkillComponent)))
                {
                    args.PushMarkup(Loc.GetString(ent.Comp.UnskilledExamine));
                }
            }

            return;
        }

        if (!TryComp(args.Examined, out SolutionContainerManagerComponent? solutionContainerManager))
            return;

        var foundReagents = new List<ReagentQuantity>();
        foreach (var solutionContainerId in solutionContainerManager.Containers)
        {
            if (!_solutionContainerSystem.TryGetSolution(args.Examined, solutionContainerId, out _, out var solution))
                continue;

            foreach (var reagent in solution.Contents)
            {
                foundReagents.Add(reagent);
            }
        }

        if (!foundReagents.Any())
        {
            using (args.PushGroup(nameof(ReagentExaminationRequiresSkillComponent)))
            {
                args.PushMarkup(Loc.GetString(ent.Comp.SkilledExamineNone));
            }

            return;
        }

        var reagentCount = foundReagents.Count;
        var fullMessage = $"{Loc.GetString(ent.Comp.SkilledExamineContains)} ";
        for (var i = 0; i < foundReagents.Count; i++)
        {
            var reagent = foundReagents[i];
            var reagentLocalizedName = _prototypeManager.Index<ReagentPrototype>(reagent.Reagent.Prototype).LocalizedName;
            var reagentQuantity = reagent.Quantity;
            fullMessage += $"{reagentLocalizedName}({reagentQuantity}u)";
            if (i > reagentCount)
                fullMessage += ", ";
        }

        using (args.PushGroup(nameof(ReagentExaminationRequiresSkillComponent)))
        {
            args.PushMarkup(fullMessage);
        }
    }

    private void OnExamineRequiresSkill(Entity<ExamineRequiresSkillComponent> ent, ref ExaminedEvent args)
    {
        if (!HasSkills(args.Examiner, in ent.Comp.Skills))
        {
            if (ent.Comp.UnskilledExamine != null)
            {
                using (args.PushGroup(nameof(ExamineRequiresSkillComponent), ent.Comp.ExaminePriority))
                {
                    args.PushMarkup(Loc.GetString(ent.Comp.UnskilledExamine));
                }
            }

            return;
        }

        using (args.PushGroup(nameof(ExamineRequiresSkillComponent), ent.Comp.ExaminePriority))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.SkilledExamine));
        }
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

        // TODO RMC14 turn these into prototypes
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
               skills.SmartGun >= required.SmartGun &&
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
