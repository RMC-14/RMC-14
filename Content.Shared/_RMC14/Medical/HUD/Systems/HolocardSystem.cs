using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.HUD.Events;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.HUD.Systems;

public sealed class HolocardSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const int MinimumRequiredSkill = 2;
    public static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        SubscribeLocalEvent<HolocardStateComponent, HolocardChangeEvent>(ChangeHolocard);
        SubscribeLocalEvent<HolocardStateComponent, GetVerbsEvent<ExamineVerb>>(OnHolocardExaminableVerb);

        SubscribeLocalEvent<HealthScannerComponent, OpenChangeHolocardUIEvent>(OpenChangeHolocardUI);
        SubscribeLocalEvent<HealthScannerComponent, RefreshEquipmentHudEvent<HealthScannerComponent>>(OnRefreshEquipmentHud);

        SubscribeLocalEvent<HolocardContainerComponent, HolocardContainerStatusUpdateEvent>(OnHolocardContainerStatusUpdate);
        SubscribeLocalEvent<HolocardContainerComponent, EntInsertedIntoContainerMessage>(OnHolocardContainerEntInserted);
        SubscribeLocalEvent<HolocardContainerComponent, EntRemovedFromContainerMessage>(OnHolocardContainerEntRemoved);
    }

    private void ChangeHolocard(Entity<HolocardStateComponent> ent, ref HolocardChangeEvent args)
    {
        if (args.UiKey is not HolocardChangeUIKey.Key)
            return;

        if (!TryGetEntity(args.Owner, out var viewer))
            return;

        if (!_transform.InRange(ent.Owner, viewer.Value, 15f))
            return;

        // A player with insufficient medical skill cannot change holocards
        if (!_skills.HasSkill(viewer.Value, SkillType, MinimumRequiredSkill))
            return;

        ent.Comp.HolocardStatus = args.NewHolocardStatus;

        if (_container.TryGetOuterContainer(ent, Transform(ent), out var container))
        {
            var ev = new HolocardContainerStatusUpdateEvent(args.NewHolocardStatus);
            RaiseLocalEvent(container.Owner, ref ev);
        }

        Dirty(ent);
    }

    private void OnHolocardExaminableVerb(Entity<HolocardStateComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        // A player with insufficient medical skill cannot change holocards
        if (!_skills.HasSkill(args.User, SkillType, MinimumRequiredSkill))
            return;

        var scanEvent = new HolocardScanEvent(false, SlotFlags.EYES | SlotFlags.HEAD);
        RaiseLocalEvent(args.User, ref scanEvent);
        if (!scanEvent.CanScan)
            return;

        var target = args.Target;
        var user = args.User;
        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _ui.OpenUi(target, HolocardChangeUIKey.Key, user);
            },
            Text = Loc.GetString("scannable-holocard-verb-text"),
            Message = Loc.GetString("scannable-holocard-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/_RMC14/Interface/VerbIcons/ambulance.png")),
        };

        args.Verbs.Add(verb);
    }

    private void OpenChangeHolocardUI(EntityUid entity, HealthScannerComponent comp, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = GetEntity(args.Owner);
        var localTarget = GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }

    private void OnRefreshEquipmentHud(Entity<HealthScannerComponent> ent, ref RefreshEquipmentHudEvent<HealthScannerComponent> args)
    {
        args.Active = true;
    }

    private void OnHolocardContainerStatusUpdate(Entity<HolocardContainerComponent> container, ref HolocardContainerStatusUpdateEvent args)
    {
        _appearance.SetData(container, HolocardContainerVisuals.State, args.NewStatus);
    }

    private void OnHolocardContainerEntInserted(Entity<HolocardContainerComponent> container, ref EntInsertedIntoContainerMessage args)
    {
        var state = HolocardStatus.None;

        if (TryComp<HolocardStateComponent>(args.Entity, out var holocard))
            state = holocard.HolocardStatus;

        _appearance.SetData(container, HolocardContainerVisuals.State, state);
    }

    private void OnHolocardContainerEntRemoved(Entity<HolocardContainerComponent> container, ref EntRemovedFromContainerMessage args)
    {
        _appearance.SetData(container, HolocardContainerVisuals.State, HolocardStatus.None);
    }
}
