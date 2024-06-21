using Content.Shared._CM14.Marines.Skills;
using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared._CM14.Medical.HUD.Events;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Medical.HUD.Systems;

public sealed class HolocardSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public const int MinimumRequiredMedicalSkill = 2;

    public override void Initialize()
    {
        SubscribeLocalEvent<HolocardStateComponent, HolocardChangeEvent>(ChangeHolocard);
        SubscribeLocalEvent<HolocardStateComponent, GetVerbsEvent<ExamineVerb>>(OnHolocardExaminableVerb);

        SubscribeLocalEvent<HealthScannerComponent, OpenChangeHolocardUIEvent>(OpenChangeHolocardUI);
        SubscribeLocalEvent<HealthScannerComponent, RefreshEquipmentHudEvent<HealthScannerComponent>>(OnRefreshEquipmentHud);
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
        if (!TryComp(viewer, out SkillsComponent? skills) ||
            skills.Skills.Medical < MinimumRequiredMedicalSkill)
        {
            return;
        }

        ent.Comp.HolocardStatus = args.NewHolocardStatus;
        Dirty(ent);
    }

    private void OnHolocardExaminableVerb(Entity<HolocardStateComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        // A player with insufficient medical skill cannot change holocards
        if (!TryComp(args.User, out SkillsComponent? skills) ||
            skills.Skills.Medical < MinimumRequiredMedicalSkill)
        {
            return;
        }

        var scanEvent = new HolocardScanEvent(false, SlotFlags.EYES);
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
            Icon = new SpriteSpecifier.Texture(new("/Textures/_CM14/Interface/VerbIcons/ambulance.png")),
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
}
