using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Surgery.Conditions;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery;

public abstract partial class SharedCMSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntProtoId, EntityUid> _surgeries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<CMSurgeryTargetComponent, CMSurgeryDoAfterEvent>(OnTargetDoAfter);

        SubscribeLocalEvent<CMSurgeryCloseIncisionConditionComponent, CMSurgeryValidEvent>(OnCloseIncisionValid);
        SubscribeLocalEvent<CMSurgeryLarvaConditionComponent, CMSurgeryValidEvent>(OnLarvaValid);
        SubscribeLocalEvent<CMSurgeryPartConditionComponent, CMSurgeryValidEvent>(OnPartConditionValid);

        InitializeSteps();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _surgeries.Clear();
    }

    private void OnTargetDoAfter(Entity<CMSurgeryTargetComponent> ent, ref CMSurgeryDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !IsSurgeryValid(ent, target, args.Surgery, args.Step, out var surgery, out var part, out var step) ||
            !PreviousStepsComplete(ent, part, surgery, args.Step) ||
            !CanPerformStep(args.User, ent, part.Comp.PartType, step, false))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            return;
        }

        var ev = new CMSurgeryStepEvent(args.User, ent, part, GetTools(args.User));
        RaiseLocalEvent(step, ref ev);

        RefreshUI(ent);
    }

    private void OnCloseIncisionValid(Entity<CMSurgeryCloseIncisionConditionComponent> ent, ref CMSurgeryValidEvent args)
    {
        if (!HasComp<CMIncisionOpenComponent>(args.Part) ||
            !HasComp<CMBleedersClampedComponent>(args.Part) ||
            !HasComp<CMSkinRetractedComponent>(args.Part))
        {
            args.Cancelled = true;
        }
    }

    private void OnLarvaValid(Entity<CMSurgeryLarvaConditionComponent> ent, ref CMSurgeryValidEvent args)
    {
        if (!TryComp(args.Body, out VictimInfectedComponent? infected))
            args.Cancelled = true;

        // The larva is bursting and surgery is now impossible
        if (infected != null && infected.IsBursting)
            args.Cancelled = true;
    }

    private void OnPartConditionValid(Entity<CMSurgeryPartConditionComponent> ent, ref CMSurgeryValidEvent args)
    {
        if (CompOrNull<BodyPartComponent>(args.Part)?.PartType != ent.Comp.Part)
            args.Cancelled = true;
    }

    protected bool IsSurgeryValid(EntityUid body, EntityUid targetPart, EntProtoId surgery, EntProtoId stepId, out Entity<CMSurgeryComponent> surgeryEnt, out Entity<BodyPartComponent> part, out EntityUid step)
    {
        surgeryEnt = default;
        part = default;
        step = default;

        if (!HasComp<CMSurgeryTargetComponent>(body) ||
            !IsLyingDown(body) ||
            !TryComp(targetPart, out BodyPartComponent? partComp) ||
            GetSingleton(surgery) is not { } surgeryEntId ||
            !TryComp(surgeryEntId, out CMSurgeryComponent? surgeryComp) ||
            !surgeryComp.Steps.Contains(stepId) ||
            GetSingleton(stepId) is not { } stepEnt)
        {
            return false;
        }

        var ev = new CMSurgeryValidEvent(body, targetPart);
        RaiseLocalEvent(stepEnt, ref ev);
        RaiseLocalEvent(surgeryEntId, ref ev);

        if (ev.Cancelled)
            return false;

        surgeryEnt = (surgeryEntId, surgeryComp);
        part = (targetPart, partComp);
        step = stepEnt;
        return true;
    }

    public EntityUid? GetSingleton(EntProtoId surgeryOrStep)
    {
        if (!_prototypes.HasIndex(surgeryOrStep))
            return null;

        // This (for now) assumes that surgery entity data remains unchanged between client
        // and server
        // if it does not you get the bullet
        if (!_surgeries.TryGetValue(surgeryOrStep, out var ent) || TerminatingOrDeleted(ent))
        {
            ent = Spawn(surgeryOrStep, MapCoordinates.Nullspace);
            _surgeries[surgeryOrStep] = ent;
        }

        return ent;
    }

    private List<EntityUid> GetTools(EntityUid surgeon)
    {
        return _hands.EnumerateHeld(surgeon).ToList();
    }

    public bool IsLyingDown(EntityUid entity)
    {
        if (_standing.IsDown(entity))
            return true;

        if (TryComp(entity, out BuckleComponent? buckle) &&
            TryComp(buckle.BuckledTo, out StrapComponent? strap))
        {
            var rotation = strap.Rotation;
            if (rotation.GetCardinalDir() is Direction.West or Direction.East)
                return true;
        }

        return false;
    }

    protected virtual void RefreshUI(EntityUid body)
    {
    }
}
