using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Surgery.Conditions;
using Content.Shared._RMC14.Medical.Surgery.Steps;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Medical.Surgery.Tools;
using Content.Shared._RMC14.Xenonids.Organs;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Nutrition.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Medical.Surgery;

public abstract partial class SharedCMSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntProtoId, EntityUid> _surgeries = new();
    private static readonly ProtoId<TagPrototype> SurfaceIdealTag = "RMCSurgerySurfaceIdeal";
    private static readonly ProtoId<TagPrototype> SurfaceAdequateTag = "RMCSurgerySurfaceAdequate";
    private static readonly ProtoId<TagPrototype> SurfaceUnsuitedTag = "RMCSurgerySurfaceUnsuited";
    private static readonly ProtoId<TagPrototype> SurfaceAwfulTag = "RMCSurgerySurfaceAwful";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<CMSurgeryTargetComponent, CMSurgeryDoAfterEvent>(OnTargetDoAfter);

        SubscribeLocalEvent<CMSurgeryCloseIncisionConditionComponent, CMSurgeryValidEvent>(OnCloseIncisionValid);
        SubscribeLocalEvent<CMSurgeryLarvaConditionComponent, CMSurgeryValidEvent>(OnLarvaValid);
        SubscribeLocalEvent<CMSurgeryPartConditionComponent, CMSurgeryValidEvent>(OnPartConditionValid);
        SubscribeLocalEvent<RMCSurgeryDeadConditionComponent, CMSurgeryValidEvent>(OnIsDead);
        SubscribeLocalEvent<RMCSurgeryXenoHeartConditionComponent, CMSurgeryValidEvent>(OnXenoHeartValid);

        InitializeSteps();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _surgeries.Clear();
    }

    private void OnTargetDoAfter(Entity<CMSurgeryTargetComponent> ent, ref CMSurgeryDoAfterEvent args)
    {
        var canonicalStep = NormalizeSurgeryStepForValidation(args.Surgery, args.Step);

        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !IsSurgeryValid(ent, target, args.Surgery, args.Step, out var surgery, out var part, out var step) ||
            !PreviousStepsComplete(ent, part, surgery, canonicalStep) ||
            !CanPerformStep(args.User, ent, part.Comp.PartType, step, false, out _, out _, out var validTools))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            return;
        }

        if (_net.IsServer &&
            !_random.Prob(args.SuccessChance))
        {
            var failEvent = new CMSurgeryStepFailedEvent(args.User, ent, part, GetTools(args.User));
            RaiseLocalEvent(step, ref failEvent);

            _popup.PopupEntity(Loc.GetString("rmc-surgery-step-failed"), args.User, PopupType.MediumCaution);
            RefreshUI(ent);
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

    private void OnIsDead(Entity<RMCSurgeryDeadConditionComponent> ent, ref CMSurgeryValidEvent args)
    {
        if (!_mobState.IsDead(args.Body))
            args.Cancelled = true;
    }

    private void OnXenoHeartValid(Entity<RMCSurgeryXenoHeartConditionComponent> ent, ref CMSurgeryValidEvent args)
    {
        if (!HasComp<RMCSurgeryXenoHeartComponent>(args.Body) ||
            _body.GetBodyOrganEntityComps<XenoHeartComponent>(args.Body).Count == 0)
            args.Cancelled = true;
    }

    protected bool IsSurgeryValid(EntityUid body, EntityUid targetPart, EntProtoId surgery, EntProtoId stepId, out Entity<CMSurgeryComponent> surgeryEnt, out Entity<BodyPartComponent> part, out EntityUid step)
    {
        surgeryEnt = default;
        part = default;
        step = default;

        var canonicalStepId = NormalizeSurgeryStepForValidation(surgery, stepId);

        if (!HasComp<CMSurgeryTargetComponent>(body) ||
            !IsLyingDown(body) ||
            !TryComp(targetPart, out BodyPartComponent? partComp) ||
            GetSingleton(surgery) is not { } surgeryEntId ||
            !TryComp(surgeryEntId, out CMSurgeryComponent? surgeryComp) ||
            !surgeryComp.Steps.Contains(canonicalStepId) ||
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

    protected static EntProtoId NormalizeSurgeryStepForValidation(EntProtoId surgeryId, EntProtoId stepId)
    {
        if (surgeryId == "CMSurgeryOpenIncision" &&
            (stepId == "RMCSurgeryStepOpenIncisionWithIMS" || stepId == "RMCSurgeryStepOpenIncisionWithLaserScalpel"))
        {
            return "CMSurgeryStepOpenIncisionScalpel";
        }

        if (surgeryId == "CMSurgeryAlienEmbryoRemoval")
        {
            if (stepId == "RMCSurgeryStepCutLarvaRootsWithPict")
                return "CMSurgeryStepCutLarvaRoots";

            if (stepId == "RMCSurgeryStepRemoveLarvaWithPict")
                return "CMSurgeryStepRemoveLarva";
        }

        return stepId;
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

    protected float GetStepSuccessChance(EntityUid body,
        EntityUid user,
        List<EntityUid>? tools,
        IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds,
        EntProtoId step)
    {
        var totalPenalty = GetToolSuitabilityScore(tools, requiredKinds, step) + GetSurfaceSuitabilityScore(body) + GetSkillCompensationScore(user);
        var failureChance = GetFailureChance(totalPenalty);
        return 1f - failureChance;
    }

    protected float GetStepDurationMultiplier(EntityUid body,
        EntityUid user,
        List<EntityUid>? tools,
        IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds,
        EntProtoId step)
    {
        var toolMultiplier = GetToolSpeedMultiplier(tools, requiredKinds, step);
        var surfaceMultiplier = GetSurfaceSpeedMultiplier(body);
        var skillMultiplier = GetSkillSpeedMultiplier(user);
        return toolMultiplier * surfaceMultiplier * skillMultiplier;
    }

    private static float GetFailureChance(int totalPenalty)
    {
        return totalPenalty switch
        {
            >= 0 => 0f,
            -1 => 0.05f,
            -2 => 0.25f,
            _ => 0.5f,
        };
    }

    private int GetSkillCompensationScore(EntityUid user)
    {
        if (!TryComp(user, out SkillsComponent? skills))
            return 0;

        if (!_skills.SkillNames.TryGetValue("RMCSkillSurgery", out var surgerySkill))
            return 0;

        Entity<SkillsComponent?> userSkills = (user, skills);
        var skillLevel = _skills.GetSkill(userSkills, surgerySkill);
        return skillLevel switch
        {
            <= 0 => 0,
            1 => 1,
            _ => 3,
        };
    }

    private float GetSkillSpeedMultiplier(EntityUid user)
    {
        if (!TryComp(user, out SkillsComponent? skills) ||
            !_skills.SkillNames.TryGetValue("RMCSkillSurgery", out var surgerySkill))
        {
            return 1.2f;
        }

        Entity<SkillsComponent?> userSkills = (user, skills);
        var skillLevel = _skills.GetSkill(userSkills, surgerySkill);
        return skillLevel switch
        {
            <= 0 => 1.2f,
            1 => 1f,
            _ => 0.6f,
        };
    }

    private int GetSurfaceSuitabilityScore(EntityUid body)
    {
        if (!TryComp(body, out BuckleComponent? buckle) || !Exists(buckle.BuckledTo))
            return -2;

        if (HasComp<CMOperatingTableComponent>(buckle.BuckledTo))
            return 0;

        if (!TryComp(buckle.BuckledTo, out TagComponent? tags))
            return -1;

        if (_tags.HasTag(tags, SurfaceIdealTag) || _tags.HasTag(tags, SurfaceAdequateTag))
            return 0;

        if (_tags.HasTag(tags, SurfaceUnsuitedTag))
            return -1;

        if (_tags.HasTag(tags, SurfaceAwfulTag))
            return -2;

        return -1;
    }

    private float GetSurfaceSpeedMultiplier(EntityUid body)
    {
        if (!TryComp(body, out BuckleComponent? buckle) || !Exists(buckle.BuckledTo))
            return 2f;

        if (HasComp<CMOperatingTableComponent>(buckle.BuckledTo))
            return 1f;

        if (!TryComp(buckle.BuckledTo, out TagComponent? tags))
            return 1.67f;

        if (_tags.HasTag(tags, SurfaceIdealTag))
            return 1f;

        if (_tags.HasTag(tags, SurfaceAdequateTag))
            return 1.33f;

        if (_tags.HasTag(tags, SurfaceUnsuitedTag))
            return 1.67f;

        if (_tags.HasTag(tags, SurfaceAwfulTag))
            return 2f;

        return 1.67f;
    }

    private int GetToolSuitabilityScore(List<EntityUid>? tools, IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds, EntProtoId step)
    {
        if (tools == null)
            return 0;

        foreach (var tool in tools)
        {
            if (TryGetToolSuitability(tool, requiredKinds, step, out var suitability, out _))
                return suitability;
        }

        return 0;
    }

    private float GetToolSpeedMultiplier(List<EntityUid>? tools, IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds, EntProtoId step)
    {
        if (tools == null)
            return 1.8f;

        foreach (var tool in tools)
        {
            if (TryGetToolSuitability(tool, requiredKinds, step, out _, out var speedMultiplier))
                return speedMultiplier;
        }

        return 1.8f;
    }

    private bool TryGetToolSuitability(EntityUid tool,
        IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds,
        EntProtoId step,
        out int suitability,
        out float speedMultiplier)
    {
        if (!TryComp(tool, out RMCSurgeryToolComponent? surgeryTool) ||
            !surgeryTool.TryGetBestTypeForStep(step, requiredKinds, out var resolved))
        {
            suitability = default;
            speedMultiplier = default;
            return false;
        }

        if (resolved.Kind == RMCSurgeryToolKind.Cautery &&
            surgeryTool.RequiresHotCautery &&
            !IsHot(tool))
        {
            suitability = default;
            speedMultiplier = default;
            return false;
        }

        speedMultiplier = resolved.Quality switch
        {
            RMCSurgeryToolQuality.Ideal => 1f,
            RMCSurgeryToolQuality.Suboptimal => 1.2f,
            RMCSurgeryToolQuality.Substitute => 1.4f,
            RMCSurgeryToolQuality.BadSubstitute => 1.6f,
            _ => 1.8f,
        };

        speedMultiplier *= resolved.SpeedMultiplier;

        suitability = resolved.Quality switch
        {
            RMCSurgeryToolQuality.Ideal => 0,
            RMCSurgeryToolQuality.Suboptimal => 0,
            RMCSurgeryToolQuality.Substitute => 0,
            RMCSurgeryToolQuality.BadSubstitute => -1,
            _ => -2,
        };

        return true;
    }

    private bool IsHot(EntityUid tool)
    {
        if (HasComp<BurningComponent>(tool))
            return true;

        var hot = new IsHotEvent();
        RaiseLocalEvent(tool, hot);
        return hot.IsHot;
    }

    protected virtual void RefreshUI(EntityUid body)
    {
    }
}
