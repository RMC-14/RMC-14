using Content.Shared._RMC14.Medical.Surgery.Conditions;
using Content.Shared._RMC14.Medical.Surgery.Steps;
using Content.Shared._RMC14.Medical.Surgery.Tools;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery;

public abstract partial class SharedCMSurgerySystem
{
    private void InitializeSteps()
    {
        SubscribeLocalEvent<CMSurgeryStepComponent, CMSurgeryStepEvent>(OnToolStep);
        SubscribeLocalEvent<CMSurgeryStepComponent, CMSurgeryStepCompleteCheckEvent>(OnToolCheck);
        SubscribeLocalEvent<CMSurgeryStepComponent, CMSurgeryCanPerformStepEvent>(OnToolCanPerform);

        SubSurgery<CMSurgeryCutLarvaRootsStepComponent>(OnCutLarvaRootsStep, OnCutLarvaRootsCheck);

        Subs.BuiEvents<CMSurgeryTargetComponent>(CMSurgeryUIKey.Key, subs =>
        {
            subs.Event<CMSurgeryStepChosenBuiMsg>(OnSurgeryTargetStepChosen);
        });
    }

    private void SubSurgery<TComp>(EntityEventRefHandler<TComp, CMSurgeryStepEvent> onStep,
        EntityEventRefHandler<TComp, CMSurgeryStepCompleteCheckEvent> onComplete) where TComp : IComponent
    {
        SubscribeLocalEvent(onStep);
        SubscribeLocalEvent(onComplete);
    }

    private void OnToolStep(Entity<CMSurgeryStepComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (ent.Comp.Tool != null)
        {
            foreach (var reg in ent.Comp.Tool.Values)
            {
                if (!AnyHaveComp(args.Tools, reg.Component, out var tool))
                    return;

                if (_net.IsServer &&
                    TryComp(tool, out CMSurgeryToolComponent? toolComp) &&
                    toolComp.EndSound != null)
                {
                    _audio.PlayPvs(toolComp.EndSound, tool);
                }
            }
        }

        if (ent.Comp.Add != null)
        {
            foreach (var reg in ent.Comp.Add.Values)
            {
                var compType = reg.Component.GetType();
                if (HasComp(args.Part, compType))
                    continue;

                AddComp(args.Part, _compFactory.GetComponent(compType));
            }
        }

        if (ent.Comp.Remove != null)
        {
            foreach (var reg in ent.Comp.Remove.Values)
            {
                RemComp(args.Part, reg.Component.GetType());
            }
        }

        if (ent.Comp.BodyRemove != null)
        {
            foreach (var reg in ent.Comp.BodyRemove.Values)
            {
                RemComp(args.Body, reg.Component.GetType());
            }
        }
    }

    private void OnToolCheck(Entity<CMSurgeryStepComponent> ent, ref CMSurgeryStepCompleteCheckEvent args)
    {
        if (ent.Comp.Add != null)
        {
            foreach (var reg in ent.Comp.Add.Values)
            {
                if (!HasComp(args.Part, reg.Component.GetType()))
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }

        if (ent.Comp.Remove != null)
        {
            foreach (var reg in ent.Comp.Remove.Values)
            {
                if (HasComp(args.Part, reg.Component.GetType()))
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }

        if (ent.Comp.BodyRemove != null)
        {
            foreach (var reg in ent.Comp.BodyRemove.Values)
            {
                if (HasComp(args.Body, reg.Component.GetType()))
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }

    private void OnToolCanPerform(Entity<CMSurgeryStepComponent> ent, ref CMSurgeryCanPerformStepEvent args)
    {
        if (!_skills.HasSkill(args.User, ent.Comp.SkillType, ent.Comp.Skill))
        {
            args.Invalid = StepInvalidReason.MissingSkills;
            return;
        }

        if (HasComp<CMSurgeryOperatingTableConditionComponent>(ent))
        {
            if (!TryComp(args.Body, out BuckleComponent? buckle) ||
                !HasComp<CMOperatingTableComponent>(buckle.BuckledTo))
            {
                args.Invalid = StepInvalidReason.NeedsOperatingTable;
                return;
            }
        }

        RaiseLocalEvent(args.Body, ref args);

        if (args.Invalid != StepInvalidReason.None)
            return;

        if (ent.Comp.Tool != null)
        {
            args.ValidTools ??= new HashSet<EntityUid>();

            foreach (var reg in ent.Comp.Tool.Values)
            {
                if (!AnyHaveComp(args.Tools, reg.Component, out var withComp))
                {
                    args.Invalid = StepInvalidReason.MissingTool;

                    if (reg.Component is ICMSurgeryToolComponent tool)
                        args.Popup = $"You need {tool.ToolName} to perform this step!";

                    return;
                }

                args.ValidTools.Add(withComp);
            }
        }
    }

    private void OnCutLarvaRootsStep(Entity<CMSurgeryCutLarvaRootsStepComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (TryComp(args.Body, out VictimInfectedComponent? infected) && !infected.IsBursting)
        {
            infected.RootsCut = true;
        }
    }

    private void OnCutLarvaRootsCheck(Entity<CMSurgeryCutLarvaRootsStepComponent> ent, ref CMSurgeryStepCompleteCheckEvent args)
    {
        if (!TryComp(args.Body, out VictimInfectedComponent? infected) || !infected.RootsCut)
            args.Cancelled = true;

        // The larva is bursting
        if (infected != null && infected.IsBursting)
            args.Cancelled = true;
    }

    private void OnSurgeryTargetStepChosen(Entity<CMSurgeryTargetComponent> ent, ref CMSurgeryStepChosenBuiMsg args)
    {
        var user = args.Actor;
        if (GetEntity(args.Entity) is not { Valid: true } body ||
            GetEntity(args.Part) is not { Valid: true } targetPart ||
            !IsSurgeryValid(body, targetPart, args.Surgery, args.Step, out var surgery, out var part, out var step))
        {
            return;
        }

        if (!PreviousStepsComplete(body, part, surgery, args.Step) ||
            IsStepComplete(body, part, args.Step))
        {
            return;
        }

        if (!CanPerformStep(user, body, part.Comp.PartType, step, true, out _, out _, out var validTools))
            return;

        if (_net.IsServer && validTools?.Count > 0)
        {
            foreach (var tool in validTools)
            {
                if (TryComp(tool, out CMSurgeryToolComponent? toolComp) &&
                    toolComp.StartSound != null)
                {
                    _audio.PlayPvs(toolComp.StartSound, tool);
                }
            }
        }

        if (TryComp(body, out TransformComponent? xform))
            _rotateToFace.TryFaceCoordinates(user, _transform.GetMapCoordinates(body, xform).Position);

        var ev = new CMSurgeryDoAfterEvent(args.Surgery, args.Step);
        var doAfter = new DoAfterArgs(EntityManager, user, 2, ev, body, part)
        {
            BreakOnMove = true,
            TargetEffect = "RMCEffectHealBusy",
            MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private (Entity<CMSurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, Entity<CMSurgeryComponent?> surgery, List<EntityUid> requirements)
    {
        if (!Resolve(surgery, ref surgery.Comp))
            return null;

        if (requirements.Contains(surgery))
            throw new ArgumentException($"Surgery {surgery} has a requirement loop: {string.Join(", ", requirements)}");

        requirements.Add(surgery);

        if (surgery.Comp.Requirement is { } requirementId &&
            GetSingleton(requirementId) is { } requirement &&
            GetNextStep(body, part, requirement, requirements) is { } requiredNext)
        {
            return requiredNext;
        }

        for (var i = 0; i < surgery.Comp.Steps.Count; i++)
        {
            var surgeryStep = surgery.Comp.Steps[i];
            if (!IsStepComplete(body, part, surgeryStep))
                return ((surgery, surgery.Comp), i);
        }

        return null;
    }

    public (Entity<CMSurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, EntityUid surgery)
    {
        return GetNextStep(body, part, surgery, new List<EntityUid>());
    }

    public bool PreviousStepsComplete(EntityUid body, EntityUid part, Entity<CMSurgeryComponent> surgery, EntProtoId step)
    {
        // TODO RMC14 use index instead of the prototype id
        if (surgery.Comp.Requirement is { } requirement)
        {
            if (GetSingleton(requirement) is not { } requiredEnt ||
                !TryComp(requiredEnt, out CMSurgeryComponent? requiredComp) ||
                !PreviousStepsComplete(body, part, (requiredEnt, requiredComp), step))
            {
                return false;
            }
        }

        foreach (var surgeryStep in surgery.Comp.Steps)
        {
            if (surgeryStep == step)
                break;

            if (!IsStepComplete(body, part, surgeryStep))
                return false;
        }

        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup, out string? popup, out StepInvalidReason reason, out HashSet<EntityUid>? validTools)
    {
        var slot = part switch
        {
            BodyPartType.Head => SlotFlags.HEAD,
            BodyPartType.Torso => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Arm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Hand => SlotFlags.GLOVES,
            BodyPartType.Leg => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
            BodyPartType.Foot => SlotFlags.FEET,
            BodyPartType.Tail => SlotFlags.NONE,
            BodyPartType.Other => SlotFlags.NONE,
            _ => SlotFlags.NONE
        };

        var check = new CMSurgeryCanPerformStepEvent(user, body, GetTools(user), slot);
        RaiseLocalEvent(step, ref check);
        popup = check.Popup;
        validTools = check.ValidTools;

        if (check.Invalid != StepInvalidReason.None)
        {
            if (doPopup && check.Popup != null)
                _popup.PopupEntity(check.Popup, user, PopupType.SmallCaution);

            reason = check.Invalid;
            return false;
        }

        reason = default;
        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup)
    {
        return CanPerformStep(user, body, part, step, doPopup, out _, out _, out _);
    }

    public bool IsStepComplete(EntityUid body, EntityUid part, EntProtoId step)
    {
        if (GetSingleton(step) is not { } stepEnt)
            return false;

        var ev = new CMSurgeryStepCompleteCheckEvent(body, part);
        RaiseLocalEvent(stepEnt, ref ev);

        return !ev.Cancelled;
    }

    private bool AnyHaveComp(List<EntityUid> tools, IComponent component, out EntityUid withComp)
    {
        foreach (var tool in tools)
        {
            if (HasComp(tool, component.GetType()))
            {
                withComp = tool;
                return true;
            }
        }

        withComp = default;
        return false;
    }
}
