using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Tools;
using Content.Shared.Interaction.Events;

namespace Content.Server._RMC14.Labeler;

public sealed class RMCHandLabelerSystem : SharedRMCHandLabelerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHandLabelerComponent, RMCChemMasterPillBottleColorMsg>(OnPillBottleColorMsg);
        SubscribeLocalEvent<RMCHandLabelerComponent, DroppedEvent>(OnHandLabelerDropped);
        SubscribeLocalEvent<RMCHandLabelerComponent, BoundUIClosedEvent>(OnManualUIClose);
    }

    protected override void OnPillBottleInteract(EntityUid labeler, EntityUid pillBottle, EntityUid user)
    {
        if (!TryComp<RMCHandLabelerComponent>(labeler, out var comp))
            return;

        comp.CurrentPillBottle = pillBottle;
        Dirty(labeler, comp);

        _ui.TryOpenUi(labeler, RMCHandLabelerUiKey.PillBottleColor, user);
    }

    private void OnPillBottleColorMsg(Entity<RMCHandLabelerComponent> ent, ref RMCChemMasterPillBottleColorMsg args)
    {
        if (!ent.Comp.CurrentPillBottle.HasValue)
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

    private void OnManualUIClose(Entity<RMCHandLabelerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(RMCHandLabelerUiKey.PillBottleColor))
        {
            ClearPillBottleReference(ent);
        }
    }

    private void CloseColorUI(Entity<RMCHandLabelerComponent> ent)
    {
        ClearPillBottleReference(ent);
        _ui.CloseUi(ent.Owner, RMCHandLabelerUiKey.PillBottleColor);
    }

    private void ClearPillBottleReference(Entity<RMCHandLabelerComponent> ent)
    {
        if (!ent.Comp.CurrentPillBottle.HasValue)
            return;
        ent.Comp.CurrentPillBottle = null;
        Dirty(ent);
    }
}
