using Content.Shared._CM14.Marines.Skills;
using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared._CM14.Medical.HUD.Events;
using Content.Shared._CM14.Medical.HUD.Systems;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared._CM14.Medical.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Medical.HUD.Systems;

public sealed class HolocardSystem : SharedHolocardSystem
{
    [Dependency] readonly private SharedUserInterfaceSystem _ui = default!;
    [Dependency] readonly private IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolocardStateComponent, HolocardChangeEvent>(ChangeHolocard);
        SubscribeLocalEvent<HealthScannerComponent, OpenChangeHolocardUIEvent>(OpenChangeHolocardUI);

        SubscribeLocalEvent<HolocardStateComponent, GetVerbsEvent<ExamineVerb>>(OnHolocardExaminableVerb);
}

    private void ChangeHolocard(EntityUid entity, HolocardStateComponent comp, ref HolocardChangeEvent args)
    {
        // A player with insufficient medical skill cannot change holocards
        var viewer = GetEntity(args.Owner);
        if (!TryComp(viewer, out SkillsComponent? skills) ||
            skills.Skills.Medical < HolocardSystem.MinimumRequiredMedicalSkill)
        {
            return;
        }
        comp.HolocardStaus = args.NewHolocardStatus;
        Dirty(entity, comp);

    }
    private void OpenChangeHolocardUI(EntityUid entity, HealthScannerComponent comp, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = _entityManager.GetEntity(args.Owner);
        var localTarget = _entityManager.GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }

    private void OnHolocardExaminableVerb(Entity<HolocardStateComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        // A player with insufficient medical skill cannot change holocards
        if (!TryComp(args.User, out SkillsComponent? skills) ||
            skills.Skills.Medical < HolocardSystem.MinimumRequiredMedicalSkill)
        {
            return;
        }

        var scanEvent = new HolocardScanEvent();
        RaiseLocalEvent(args.User, scanEvent);
        if (!scanEvent.CanScan)
        {
            return;
        }

        var target = args.Target;
        var user = args.User;
        var @event = args;
        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _ui.OpenUi(target, HolocardChangeUIKey.Key, @event.User);
            },
            Text = Loc.GetString("scannable-holocard-verb-text"),
            Message = Loc.GetString("scannable-holocard-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/_CM14/Interface/VerbIcons/ambulance.png")),
        };

        args.Verbs.Add(verb);
    }
}
