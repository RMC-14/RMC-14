using Content.Server._RMC14.Medical.Wounds;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Surgery;
using Content.Shared._RMC14.Medical.Surgery.Conditions;
using Content.Shared._RMC14.Medical.Surgery.Effects.Step;
using Content.Shared._RMC14.Medical.Surgery.Tools;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Organs;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Interaction;
using Content.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly WoundsSystem _wounds = default!;

    private readonly List<EntProtoId> _surgeries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMSurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);

        SubscribeLocalEvent<CMSurgeryStepBleedEffectComponent, CMSurgeryStepEvent>(OnStepBleedComplete);
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

    private void OnToolAfterInteract(Entity<CMSurgeryToolComponent> ent, ref AfterInteractEvent args)
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

        if (user == args.Target)
        {
            _popup.PopupEntity("You can't perform surgery on yourself!", user, user);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(args.Target.Value, CMSurgeryUIKey.Key, user);

        RefreshUI(args.Target.Value);
    }

    private void OnStepBleedComplete(Entity<CMSurgeryStepBleedEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        _wounds.AddWound(args.Body, ent.Comp.Damage, WoundType.Surgery, TimeSpan.MaxValue);
    }

    private void OnStepClampBleedComplete(Entity<CMSurgeryClampBleedEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        _wounds.RemoveWounds(args.Body, WoundType.Surgery);
    }

    private void OnStepScreamComplete(Entity<CMSurgeryStepEmoteEffectComponent> ent, ref CMSurgeryStepEvent args)
    {
        _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
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
