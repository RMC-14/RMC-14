﻿using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> _meleeSkill = "RMCSkillMeleeWeapons";

    public ImmutableArray<EntProtoId<SkillDefinitionComponent>> Skills { get; private set; }

    public ImmutableDictionary<string, EntProtoId<SkillDefinitionComponent>> SkillNames { get; private set; } =
        ImmutableDictionary<string, EntProtoId<SkillDefinitionComponent>>.Empty;

    private EntityQuery<SkillsComponent> _skillsQuery;

    public override void Initialize()
    {
        _skillsQuery = GetEntityQuery<SkillsComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<GetMeleeDamageEvent>(OnGetMeleeDamage);

        SubscribeLocalEvent<MedicallyUnskilledDoAfterComponent, AttemptHyposprayUseEvent>(OnAttemptHyposprayUse);

        SubscribeLocalEvent<RequiresSkillComponent, BeforeRangedInteractEvent>(OnRequiresSkillBeforeRangedInteract);
        SubscribeLocalEvent<RequiresSkillComponent, ActivatableUIOpenAttemptEvent>(OnRequiresSkillActivatableUIOpenAttempt);
        SubscribeLocalEvent<RequiresSkillComponent, UseInHandEvent>(OnRequiresSkillUseInHand, before: [typeof(SharedHypospraySystem), typeof(SharedFlashSystem)]);

        SubscribeLocalEvent<MeleeRequiresSkillComponent, AttemptMeleeEvent>(OnMeleeRequiresSkillAttemptMelee);
        SubscribeLocalEvent<MeleeRequiresSkillComponent, ThrowItemAttemptEvent>(OnMeleeRequiresSkillThrowAttempt);
        SubscribeLocalEvent<MeleeRequiresSkillComponent, UseInHandEvent>(OnMeleeRequiresSkillUseInHand, before: [typeof(SharedHypospraySystem), typeof(SharedFlashSystem)]);

        SubscribeLocalEvent<ItemToggleRequiresSkillComponent, ItemToggleActivateAttemptEvent>(OnItemToggleRequiresSkill);
        SubscribeLocalEvent<ItemToggleDeactivateUnskilledComponent, GotEquippedEvent>(OnItemToggleDeactivateUnskilled);

        SubscribeLocalEvent<ReagentExaminationRequiresSkillComponent, ExaminedEvent>(OnExamineReagentContainer);

        SubscribeLocalEvent<ExamineRequiresSkillComponent, ExaminedEvent>(OnExamineRequiresSkill);

        ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPrototypes();
    }

    private void OnGetMeleeDamage(ref GetMeleeDamageEvent args)
    {
        if (args.User == args.Weapon)
            return;

        var skill = GetSkill(args.User, _meleeSkill);
        if (skill <= 0)
            return;

        args.Damage *= 1 + 0.25 * skill;
    }

    private void OnAttemptHyposprayUse(Entity<MedicallyUnskilledDoAfterComponent> ent, ref AttemptHyposprayUseEvent args)
    {
        if (!HasSkill(args.User, ent.Comp.Skill, ent.Comp.Min))
        {
            args.MaxDoAfter(ent.Comp.DoAfter);
        }
    }

    private void OnRequiresSkillBeforeRangedInteract(Entity<RequiresSkillComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", args.Used));
            _popup.PopupClient(msg, args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnRequiresSkillActivatableUIOpenAttempt(Entity<RequiresSkillComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-no-training", ("target", ent));
            _popup.PopupClient(msg, args.User, PopupType.SmallCaution);
            args.Cancel();
        }
    }

    private void OnRequiresSkillUseInHand(Entity<RequiresSkillComponent> ent, ref UseInHandEvent args)
    {
        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnMeleeRequiresSkillAttemptMelee(Entity<MeleeRequiresSkillComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
            args.Cancelled = true;
        }
    }

    private void OnMeleeRequiresSkillThrowAttempt(Entity<MeleeRequiresSkillComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            if (_net.IsServer)
            {
                var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
                _popup.PopupEntity(msg, args.User, args.User, PopupType.SmallCaution);
            }

            args.Cancelled = true;
        }
    }

    private void OnMeleeRequiresSkillUseInHand(Entity<MeleeRequiresSkillComponent> ent, ref UseInHandEvent args)
    {
        if (!HasAllSkills(args.User, ent.Comp.Skills))
        {
            var msg = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
        }
    }

    private void OnItemToggleRequiresSkill(Entity<ItemToggleRequiresSkillComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!HasAllSkills(args.User.Value, ent.Comp.Skills))
        {
            args.Popup = Loc.GetString("rmc-skills-cant-use", ("item", ent));
            args.Cancelled = true;
        }
    }

    private void OnItemToggleDeactivateUnskilled(Entity<ItemToggleDeactivateUnskilledComponent> ent, ref GotEquippedEvent args)
    {
        if (!HasAllSkills(args.Equipee, ent.Comp.Skills))
        {
            if (_toggle.IsActivated(ent.Owner) && _toggle.TryDeactivate(ent.Owner, args.Equipee) && ent.Comp.Popup != null)
            {
                var msg = Loc.GetString(ent.Comp.Popup, ("item", ent));
                _popup.PopupClient(msg, args.Equipee, args.Equipee, PopupType.SmallCaution);
            }
        }
    }

    private void OnExamineReagentContainer(Entity<ReagentExaminationRequiresSkillComponent> ent, ref ExaminedEvent args)
    {
        if (!HasAllSkills(args.Examiner, ent.Comp.Skills))
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
            var reagentLocalizedName = _prototypes.Index<ReagentPrototype>(reagent.Reagent.Prototype).LocalizedName;
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
        if (!HasAllSkills(args.Examiner, ent.Comp.Skills))
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

    private void ReloadPrototypes()
    {
        var skillsArray = ImmutableArray.CreateBuilder<EntProtoId<SkillDefinitionComponent>>();
        var skillsDict = ImmutableDictionary.CreateBuilder<string, EntProtoId<SkillDefinitionComponent>>();
        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.HasComponent<SkillDefinitionComponent>())
                continue;

            var id = prototype.ID;
            skillsArray.Add(id);
            if (!skillsDict.TryAdd(prototype.Name, id))
            {
                var old = skillsDict.GetValueOrDefault(prototype.Name).Id;
                var msg = $"Duplicate skill name found: {prototype.Name}, old: {old}, new: {id}";

                Log.Error(msg);
                DebugTools.Assert(msg);
            }
        }

        Skills = skillsArray.ToImmutable();
        SkillNames = skillsDict.ToImmutable();
    }

    public TimeSpan GetDelay(EntityUid user, EntityUid tool)
    {
        if (!TryComp(tool, out MedicallyUnskilledDoAfterComponent? doAfter) ||
            doAfter.Min <= 0)
        {
            return default;
        }

        if (!HasSkill(user, doAfter.Skill, doAfter.Min))
            return doAfter.DoAfter;

        return default;
    }

    public int GetSkill(Entity<SkillsComponent?> ent, EntProtoId<SkillDefinitionComponent> skill)
    {
        if (skill == default)
        {
            var msg = $"Empty skill {skill} passed to {nameof(GetSkill)}!";
            Log.Error(msg);
            DebugTools.Assert(msg);
        }

        if (!_skillsQuery.Resolve(ent, ref ent.Comp, false))
            return 0;

        return ent.Comp.Skills.GetValueOrDefault(skill);
    }

    public bool HasSkills(Entity<SkillsComponent?> ent, SkillWhitelist whitelist)
    {
        return HasAllSkills(ent, whitelist.All);
    }

    public bool HasAllSkills(Entity<SkillsComponent?> ent, Dictionary<EntProtoId<SkillDefinitionComponent>, int> required)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        _skillsQuery.Resolve(ent, ref ent.Comp, false);
        foreach (var (requiredSkill, requiredLevel) in required)
        {
            if (requiredLevel <= 0)
                continue;

            if (ent.Comp == null)
                return false;

            if (!ent.Comp.Skills.TryGetValue(requiredSkill, out var level) ||
                level < requiredLevel)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasAllSkills(Entity<SkillsComponent?> ent, List<Skill> allRequired)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        _skillsQuery.Resolve(ent, ref ent.Comp, false);

        var span = CollectionsMarshal.AsSpan(allRequired);
        foreach (ref var required in span)
        {
            if (required.Level <= 0)
                continue;

            if (ent.Comp == null)
                return false;

            if (!ent.Comp.Skills.TryGetValue(required.Type, out var level) ||
                level < required.Level)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasAnySkills(Entity<SkillsComponent?> ent, Dictionary<EntProtoId<SkillDefinitionComponent>, int> anyRequired)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        _skillsQuery.Resolve(ent, ref ent.Comp, false);
        foreach (var (requiredSkill, requiredLevel) in anyRequired)
        {
            if (requiredLevel <= 0)
                continue;

            if (ent.Comp != null &&
                ent.Comp.Skills.TryGetValue(requiredSkill, out var level) &&
                level >= requiredLevel)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasAnySkills(Entity<SkillsComponent?> ent, List<Skill> anyRequired)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        _skillsQuery.Resolve(ent, ref ent.Comp, false);

        var span = CollectionsMarshal.AsSpan(anyRequired);
        foreach (ref var required in span)
        {
            if (required.Level <= 0)
                continue;

            if (ent.Comp != null &&
                ent.Comp.Skills.TryGetValue(required.Type, out var level) &&
                level >= required.Level)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasSkill(Entity<SkillsComponent?> ent, EntProtoId<SkillDefinitionComponent> skill, int required)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        if (required <= 0)
            return true;

        return _skillsQuery.Resolve(ent, ref ent.Comp, false) &&
               ent.Comp.Skills.TryGetValue(skill, out var level) &&
               level >= required;
    }

    public void IncrementSkill(Entity<SkillsComponent?> ent, EntProtoId<SkillDefinitionComponent> skill, int by = 1)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);
        SetSkill(ent, skill, ent.Comp.Skills.GetValueOrDefault(skill) + by);
    }

    public void IncrementSkills(Entity<SkillsComponent?> ent, List<EntProtoId<SkillDefinitionComponent>> skills, int by = 1)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);

        var span = CollectionsMarshal.AsSpan(skills);
        foreach (ref var skill in span)
        {
            SetSkill(ent, skill, ent.Comp.Skills.GetValueOrDefault(skill) + by);
        }
    }

    public void IncrementSkills(Entity<SkillsComponent?> ent, HashSet<EntProtoId<SkillDefinitionComponent>> skills, int by = 1)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);

        foreach (var skill in skills)
        {
            SetSkill(ent, skill, ent.Comp.Skills.GetValueOrDefault(skill) + by);
        }
    }

    public void RemoveAllSkills(Entity<SkillsComponent?> ent)
    {
        if (!_skillsQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Skills.Clear();
        Dirty(ent);
    }

    public void SetSkill(Entity<SkillsComponent?> ent, EntProtoId<SkillDefinitionComponent> skill, int to)
    {
        if (skill == default)
        {
            var msg = $"Empty skill {skill} passed to {nameof(SetSkill)}!";
            Log.Error(msg);
            DebugTools.Assert(msg);
            return;
        }

        DebugTools.Assert(_prototypes.TryIndex(skill, out var entProto) &&
                     entProto.HasComponent<SkillDefinitionComponent>());

        ent.Comp ??= EnsureComp<SkillsComponent>(ent);
        ent.Comp.Skills[skill] = to;
        Dirty(ent);
    }

    public void SetSkills(Entity<SkillsComponent?> ent, Dictionary<EntProtoId<SkillDefinitionComponent>, int> to)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);

        foreach (var (skill, level) in to)
        {
            ent.Comp.Skills[skill] = level;
        }

        Dirty(ent);
    }

    public void SetSkills(Entity<SkillsComponent?> ent, List<Skill> to)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);

        var span = CollectionsMarshal.AsSpan(to);
        foreach (ref var skill in span)
        {
            ent.Comp.Skills[skill.Type] = skill.Level;
        }

        Dirty(ent);
    }

    public void SetSkills(Entity<SkillsComponent?> ent, HashSet<Skill> to)
    {
        ent.Comp ??= EnsureComp<SkillsComponent>(ent);

        foreach (var skill in to)
        {
            ent.Comp.Skills[skill.Type] = skill.Level;
        }

        Dirty(ent);
    }

    public float GetSkillDelayMultiplier(Entity<SkillsComponent?> user, EntProtoId<SkillDefinitionComponent> definition)
    {
        if (!definition.TryGet(out var definitionComp, _prototypes, _compFactory))
            return 1f;

        if (definitionComp.DelayMultipliers.Length == 0)
            return 1f;

        var skill = GetSkill(user, definition);
        if (!definitionComp.DelayMultipliers.TryGetValue(skill, out var multiplier))
            multiplier = definitionComp.DelayMultipliers[^1];

        return multiplier;
    }
}
