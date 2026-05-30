using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Tools;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;

namespace Content.Server._RMC14.Labeler;

public sealed class RMCHandLabelerSystem : SharedRMCHandLabelerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<RMCHandLabelerComponent>(RMCHandLabelerUiKey.PillBottleColor,
            subs =>
        {
            subs.Event<RMCChemMasterPillBottleColorMsg>(OnPillBottleColorMsg);
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
        });
        SubscribeLocalEvent<RMCHandLabelerComponent, DroppedEvent>(OnHandLabelerDropped);
    }

    protected override void OnPillBottleInteract(Entity<RMCHandLabelerComponent> labeler, EntityUid pillBottle, EntityUid user)
    {
        labeler.Comp.CurrentPillBottle = pillBottle;
        Dirty(labeler);
        _ui.TryOpenUi(labeler.Owner, RMCHandLabelerUiKey.PillBottleColor, user);
    }

    private void OnPillBottleColorMsg(Entity<RMCHandLabelerComponent> ent, ref RMCChemMasterPillBottleColorMsg args)
    {
        if (!_hands.IsHolding(args.Actor, ent.Owner, out _) || !ent.Comp.CurrentPillBottle.HasValue)
        {
            CloseColorUI(ent);
            return;
        }

        var pillBottle = ent.Comp.CurrentPillBottle.Value;
        if (!Exists(pillBottle) || !TryComp<AppearanceComponent>(pillBottle, out var appearance))
        {
            CloseColorUI(ent);
            return;
        }

        _appearance.SetData(pillBottle, RMCPillBottleVisuals.Color, args.Color, appearance);
        CloseColorUI(ent);
    }

    private void OnHandLabelerDropped(Entity<RMCHandLabelerComponent> ent, ref DroppedEvent args)
    {
        CloseColorUI(ent);
    }

    private void CloseColorUI(Entity<RMCHandLabelerComponent> ent)
    {
        if (ent.Comp.CurrentPillBottle.HasValue)
        {
            ent.Comp.CurrentPillBottle = null;
            Dirty(ent);
        }
        _ui.CloseUi(ent.Owner, RMCHandLabelerUiKey.PillBottleColor);
    }

    private void OnBoundUIClosed(Entity<RMCHandLabelerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!ent.Comp.CurrentPillBottle.HasValue)
            return;
        ent.Comp.CurrentPillBottle = null;
        Dirty(ent);
    }
}
