using Content.Server._RMC14.Medical.Wounds;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Surgery;
using Content.Shared._RMC14.Medical.Surgery.Conditions;
using Content.Shared._RMC14.Medical.Surgery.Effects.Step;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Medical.Surgery.Tools;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Organs;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly WoundsSystem _wounds = default!;

    private readonly List<EntProtoId> _surgeries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);

        SubscribeLocalEvent<RMCSurgeryComplicationEffectsComponent, CMSurgeryStepEvent>(OnStepBleedComplete);
        SubscribeLocalEvent<RMCSurgeryComplicationEffectsComponent, CMSurgeryStepFailedEvent>(OnStepBleedFailed);
        SubscribeLocalEvent<RMCSurgeryLaserScalpelClampChanceEffectComponent, CMSurgeryStepEvent>(OnLaserScalpelStepComplete);
        SubscribeLocalEvent<CMSurgeryClampBleedEffectComponent, CMSurgeryStepEvent>(OnStepClampBleedComplete);
        SubscribeLocalEvent<CMSurgeryStepEmoteEffectComponent, CMSurgeryStepEvent>(OnStepScreamComplete);
        SubscribeLocalEvent<RMCSurgeryStepSpawnEffectComponent, CMSurgeryStepEvent>(OnStepSpawnComplete);
        SubscribeLocalEvent<RMCSurgeryStepLarvaEffectComponent, CMSurgeryStepEvent>(OnStepLarvaComplete);
        SubscribeLocalEvent<RMCSurgeryStepXenoHeartEffectComponent, CMSurgeryStepEvent>(OnStepXenoHeartComplete);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    protected override void RefreshUI(EntityUid body)
    {
        if (!HasComp<CMSurgeryTargetComponent>(body))
            return;

        var surgeries = new Dictionary<NetEntity, List<EntProtoId>>();
        foreach (var surgery in _surgeries)
        {
            if (GetSingleton(surgery) is not { } surgeryEnt)
                continue;

            foreach (var part in _body.GetBodyChildren(body))
            {
                var ev = new CMSurgeryValidEvent(body, part.Id);
                RaiseLocalEvent(surgeryEnt, ref ev);

                if (ev.Cancelled)
                    continue;

                surgeries.GetOrNew(GetNetEntity(part.Id)).Add(surgery);
            }
        }

        _ui.SetUiState(body, CMSurgeryUIKey.Key, new CMSurgeryBuiState(surgeries));
    }

    private void OnToolAfterInteract(Entity<RMCSurgeryToolComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !args.CanReach ||
            args.Target == null ||
            !HasComp<CMSurgeryTargetComponent>(args.Target))
        {
            return;
        }

        if (!_skills.HasSkill(user, ent.Comp.SkillType, ent.Comp.Skill))
        {
            _popup.PopupEntity("You don't know how to perform surgery!", user, user);
            return;
        }

        if (!ent.Comp.OpenSurgeryMenu)
            return;

        if (user == args.Target)
        {
            _popup.PopupEntity("You can't perform surgery on yourself!", user, user);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(args.Target.Value, CMSurgeryUIKey.Key, user);

        RefreshUI(args.Target.Value);
    }

    private void OnStepBleedComplete(Entity<RMCSurgeryComplicationEffectsComponent> ent, ref CMSurgeryStepEvent args)
    {
        ApplyComplicationDamage(
            args.Body,
            args.Part,
            ent.Comp.SuccessBleedDamage,
            ent.Comp.SuccessDirectDamage);

        ApplyComplicationSplash(ent.Owner, args.Body, args.User, ent.Comp, failed: false);
    }

    private void OnStepBleedFailed(Entity<RMCSurgeryComplicationEffectsComponent> ent, ref CMSurgeryStepFailedEvent args)
    {
        ApplyComplicationDamage(
            args.Body,
            args.Part,
            ent.Comp.FailureBleedDamage,
            ent.Comp.FailureDirectDamage);

        ApplyComplicationSplash(ent.Owner, args.Body, args.User, ent.Comp, failed: true);
    }

    private void OnLaserScalpelStepComplete(Entity<RMCSurgeryLaserScalpelClampChanceEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (!TryGetLaserScalpelClampChance(args.Tools, "RMCSurgeryStepOpenIncisionWithLaserScalpel", out var chance) ||
            chance <= 0f ||
            !_random.Prob(chance) ||
            HasComp<CMBleedersClampedComponent>(args.Part))
        {
            return;
        }

        AddComp<CMBleedersClampedComponent>(args.Part);
    }

    private void ApplyComplicationDamage(EntityUid body, EntityUid part, int? bleedDamage, DamageSpecifier? directDamage)
    {
        if (bleedDamage is > 0)
            _wounds.AddWound(body, bleedDamage.Value, WoundType.Surgery, TimeSpan.MaxValue);

        if (directDamage is not { DamageDict.Count: > 0 })
            return;

        // Surgery complication damage is internal, so it should not be reduced by worn armor.
        var delta = _damageable.TryChangeDamage(body, new DamageSpecifier(directDamage), ignoreResistances: true);

        if (delta is null || !delta.AnyPositive())
            _damageable.TryChangeDamage(part, new DamageSpecifier(directDamage), ignoreResistances: true);
    }

    private void ApplyComplicationSplash(EntityUid step, EntityUid body, EntityUid user, RMCSurgeryComplicationEffectsComponent effects, bool failed)
    {
        var isStandardLarvaRoots = MetaData(step).EntityPrototype?.ID == "CMSurgeryStepCutLarvaRoots";

        var enabled = failed
            ? effects.FailureSplashEnabled
            : effects.SuccessSplashEnabled;

        if (!enabled && isStandardLarvaRoots)
            enabled = true;

        if (!enabled)
        {
            return;
        }

        if (!TryComp(body, out TransformComponent? xform))
            return;

        var splashRadius = effects.SplashRadius;
        var splashDamage = effects.SplashDamage;
        var splashDecalSpawner = effects.SplashDecalSpawner;
        var splashAffectsBody = effects.SplashAffectsBody;

        if (isStandardLarvaRoots)
        {
            if (splashRadius <= 0f)
                splashRadius = 1.5f;

            splashAffectsBody = true;

            splashDecalSpawner ??= "RMCDecalSpawnerAcidBloodSplash";

            if (splashDamage is not { DamageDict.Count: > 0 })
                splashDamage = effects.FailureDirectDamage;

            if (splashDamage is not { DamageDict.Count: > 0 })
            {
                splashDamage = new DamageSpecifier
                {
                    DamageDict =
                    {
                        ["Heat"] = 12,
                    },
                };
            }
        }

        if (splashDecalSpawner is { } spawner)
            Spawn(spawner, xform.Coordinates);

        _audio.PlayPvs(effects.SplashSound, body);

        if (splashRadius <= 0f ||
            splashDamage is not { DamageDict.Count: > 0 })
        {
            return;
        }

        foreach (var target in _entityLookup.GetEntitiesInRange(xform.Coordinates, splashRadius))
        {
            if (target == body && !splashAffectsBody)
                continue;

            if (!effects.SplashAffectsXenos && HasComp<XenoComponent>(target))
                continue;

            _damageable.TryChangeDamage(target, new DamageSpecifier(splashDamage));
        }
    }

    private bool TryGetLaserScalpelClampChance(List<EntityUid> tools, EntProtoId stepId, out float chance)
    {
        foreach (var tool in tools)
        {
            if (!TryComp(tool, out RMCSurgeryToolComponent? toolComp) || toolComp is null)
                continue;

            foreach (var toolType in toolComp.ToolTypes)
            {
                if (toolType.Kind != RMCSurgeryToolKind.LaserScalpel)
                    continue;

                if (toolType.StepOverrides is null)
                {
                    chance = 0f;
                    return false;
                }

                foreach (var stepOverride in toolType.StepOverrides)
                {
                    if (stepOverride.Step != stepId || stepOverride.ClampBleedersChance is not { } overrideChance)
                        continue;

                    chance = overrideChance;
                    return true;
                }

                // We found a valid laser scalpel tool but no clamp override for this step.
                chance = 0f;
                return false;
            }
        }

        chance = 0f;
        return false;
    }

    private void OnStepClampBleedComplete(Entity<CMSurgeryClampBleedEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        _wounds.RemoveWounds(args.Body, WoundType.Surgery);
    }

    private void OnStepScreamComplete(Entity<CMSurgeryStepEmoteEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (!HasComp<RMCUnconsciousComponent>(args.Body))
        {
            _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
        }
    }

    private void OnStepSpawnComplete(Entity<RMCSurgeryStepSpawnEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Entity, xform.Coordinates);
    }

    private void OnStepLarvaComplete(Entity<RMCSurgeryStepLarvaEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (!TryComp<VictimInfectedComponent>(args.Body, out var infected))
            return;

        if (!TryComp(args.Body, out TransformComponent? xform))
            return;

        var coords = xform.Coordinates;

        if (infected.SpawnedLarva != null)
        {
            if (_container.TryGetContainer(args.Body, infected.LarvaContainerId, out var container))
            {
                foreach (var larva in container.ContainedEntities)
                    RemCompDeferred<BursterComponent>(larva);
                _container.EmptyContainer(container, destination: coords);
            }
        }
        else
        {
            SpawnAtPosition(ent.Comp.DeadLarvaItem, coords);
        }
    }

    private void OnStepXenoHeartComplete(Entity<RMCSurgeryStepXenoHeartEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<RMCSurgeryXenoHeartComponent>(args.Body, out var heart))
            return;

        if (!TryComp(args.Body, out TransformComponent? xform))
            return;

        foreach (var entity in _body.GetBodyOrganEntityComps<XenoHeartComponent>(args.Body))
        {
            QueueDel(entity.Owner);
        }

        SpawnAtPosition(heart.Item, xform.Coordinates);
        RemCompDeferred<RMCSurgeryXenoHeartComponent>(args.Body);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _surgeries.Clear();

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<CMSurgeryComponent>())
                _surgeries.Add(new EntProtoId(entity.ID));
        }
    }
}
