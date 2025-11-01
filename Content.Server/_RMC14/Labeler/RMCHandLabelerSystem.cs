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

        SubscribeLocalEvent<RMCHandLabelerComponent, RMCHandLabelerPillBottleColorMsg>(OnPillBottleColorMsg);
        SubscribeLocalEvent<RMCHandLabelerComponent, DroppedEvent>(OnHandLabelerDropped);
    }

    private void OnHandLabelerDropped(Entity<RMCHandLabelerComponent> ent, ref DroppedEvent args)
    {
        _ui.CloseUi(ent.Owner, RMCHandLabelerUiKey.PillBottleColor);
    }

    protected override void OnPillBottleInteract(EntityUid labeler, EntityUid pillBottle, EntityUid user)
    {
        if (!TryComp<RMCHandLabelerComponent>(labeler, out var comp))
            return;

        comp.CurrentPillBottle = pillBottle;
        Dirty(labeler, comp);

        _ui.TryOpenUi(labeler, RMCHandLabelerUiKey.PillBottleColor, user);
    }

    private void OnPillBottleColorMsg(Entity<RMCHandLabelerComponent> ent, ref RMCHandLabelerPillBottleColorMsg args)
    {
        if (!TryGetEntity(args.PillBottle, out var pillBottle))
            return;

        if (!TryComp<AppearanceComponent>(pillBottle.Value, out var appearance))
            return;

        _appearance.SetData(pillBottle.Value, RMCPillBottleVisuals.Color, args.Color, appearance);

        _ui.CloseUi(ent.Owner, RMCHandLabelerUiKey.PillBottleColor);
    }
}
