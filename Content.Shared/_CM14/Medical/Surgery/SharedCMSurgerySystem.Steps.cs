using Content.Shared._CM14.Medical.Surgery.Steps;
using Content.Shared._CM14.Medical.Surgery.Tools;
using Content.Shared._CM14.Xenos.Hugger;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical.Surgery;

public abstract partial class SharedCMSurgerySystem
{
    private void InitializeSteps()
    {
        SubscribeLocalEvent<CMSurgeryToolStepComponent, CMSurgeryStepEvent>(OnToolStep);
        SubscribeLocalEvent<CMSurgeryToolStepComponent, CMSurgeryStepCompleteCheckEvent>(OnToolCheck);
        SubscribeLocalEvent<CMSurgeryToolStepComponent, CMSurgeryCanPerformStepEvent>(OnToolCanPerform);

        SubSurgery<CMSurgeryCutLarvaRootsStepComponent>(OnCutLarvaRootsStep, OnCutLarvaRootsCheck);

        Subs.BuiEvents<CMSurgeryTargetComponent>(CMSurgeryUIKey.Key, sub =>
        {
            sub.Event<CMSurgeryStepChosenBuiMessage>(OnSurgeryTargetStepChosen);
        });
    }

    private void SubSurgery<TComp>(EntityEventRefHandler<TComp, CMSurgeryStepEvent> onStep,
        EntityEventRefHandler<TComp, CMSurgeryStepCompleteCheckEvent> onComplete) where TComp : IComponent
    {
        SubscribeLocalEvent(onStep);
        SubscribeLocalEvent(onComplete);
    }

    private void OnToolStep(Entity<CMSurgeryToolStepComponent> ent, ref CMSurgeryStepEvent args)
    {
        foreach (var reg in ent.Comp.Tool.Values)
        {
            if (!AnyHaveComp(args.Tools, reg.Component, out var tool))
                return;

            if (_net.IsServer &&
                TryComp(tool, out CMSurgeryToolComponent? toolComp) &&
                toolComp.Sound != null)
            {
                _audio.PlayEntity(toolComp.Sound, args.User, tool);
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

    private void OnToolCheck(Entity<CMSurgeryToolStepComponent> ent, ref CMSurgeryStepCompleteCheckEvent args)
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

    private void OnToolCanPerform(Entity<CMSurgeryToolStepComponent> ent, ref CMSurgeryCanPerformStepEvent args)
    {
        foreach (var reg in ent.Comp.Tool.Values)
        {
            if (!AnyHaveComp(args.Tools, reg.Component, out _))
            {
                args.Cancelled = true;

                if (reg.Component is ICMSurgeryToolComponent tool)
                    args.Popup = $"You need {tool.ToolName} to perform this step!";

                return;
            }
        }
    }

    private void OnCutLarvaRootsStep(Entity<CMSurgeryCutLarvaRootsStepComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (TryComp(args.Body, out VictimHuggedComponent? hugged) &&
            hugged.BurstAt > _timing.CurTime)
        {
            hugged.RootsCut = true;
        }
    }

    private void OnCutLarvaRootsCheck(Entity<CMSurgeryCutLarvaRootsStepComponent> ent, ref CMSurgeryStepCompleteCheckEvent args)
    {
        if (!TryComp(args.Body, out VictimHuggedComponent? hugged) || !hugged.RootsCut)
            args.Cancelled = true;
    }

    private void OnSurgeryTargetStepChosen(Entity<CMSurgeryTargetComponent> ent, ref CMSurgeryStepChosenBuiMessage args)
    {
        if (args.Session.AttachedEntity is not { } user ||
            GetEntity(args.Entity) is not { Valid: true } body ||
            !IsSurgeryValid(body, args.Part, args.Surgery, args.Step, out var surgery, out var part, out var step))
        {
            return;
        }

        if (!PreviousStepsComplete(body, part, surgery, args.Step) ||
            IsStepComplete(body, part, args.Step))
        {
            return;
        }

        if (!CanPerformStep(user, step, true))
            return;

        var ev = new CMSurgeryDoAfterEvent(GetNetEntity(part), args.Surgery, args.Step);
        var doAfter = new DoAfterArgs(EntityManager, user, 2, ev, body, body)
        {
            BreakOnMove = true
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
        // TODO CM14 use index instead of the prototype id
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

    public bool CanPerformStep(EntityUid user, EntityUid step, bool doPopup, out string? popup)
    {
        var check = new CMSurgeryCanPerformStepEvent(GetTools(user));
        RaiseLocalEvent(step, ref check);
        popup = check.Popup;

        if (check.Cancelled)
        {
            if (doPopup && check.Popup != null)
                _popup.PopupEntity(check.Popup, user, PopupType.SmallCaution);

            return false;
        }

        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid step, bool doPopup)
    {
        return CanPerformStep(user, step, doPopup, out _);
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
