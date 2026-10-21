using Content.Server.Popups;
using Content.Shared._RMC14.Botany;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantSwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCPlantMutationSystem _plantMutation = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCBotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCBotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RMCBotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<RMCBotanySwabComponent> swab, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(swab.Comp.CapturedComponents != null ? "swab-used" : "swab-unused"));
    }

    private void OnAfterInteract(Entity<RMCBotanySwabComponent> swab, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<RMCPlantTrayComponent>(args.Target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.Comp.SwabDelay, new BotanySwabDoAfterEvent(), swab.Owner, target: args.Target, used: swab.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(Entity<RMCBotanySwabComponent> swab, ref BotanySwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target || !TryComp(target, out RMCPlantTrayComponent? tray))
            return;

        if (tray.PlantSlot.ContainedEntity is not { } plant)
            return;

        if (swab.Comp.CapturedComponents == null)
        {
            swab.Comp.CapturedComponents = _plantSeed.SnapshotMutableComponents(plant);
            _popup.PopupEntity(Loc.GetString("botany-swab-from"), target, args.Args.User);
        }
        else
        {
            var oldSnapshot = _plantSeed.SnapshotMutableComponents(plant);
            _plantMutation.Cross(swab.Comp.CapturedComponents, plant);
            swab.Comp.CapturedComponents = oldSnapshot;
            _popup.PopupEntity(Loc.GetString("botany-swab-to"), target, args.Args.User);
        }

        args.Handled = true;
    }
}
