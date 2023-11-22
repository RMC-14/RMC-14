using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._CM14.Xenos.Walker;

public sealed class XenoResinWalkerSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoResinWalkerComponent, XenoResinWalkerActionEvent>(OnXenoResinWalkerAction);
        SubscribeLocalEvent<XenoResinWalkerComponent, RefreshMovementSpeedModifiersEvent>(OnXenoResinWalkerRefreshMovementSpeed);
        SubscribeLocalEvent<XenoResinWalkerComponent, XenoOnWeedsChangedEvent>(OnXenoResinWalkerOnWeedsUpdated);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnXenoResinWalkerAction(Entity<XenoResinWalkerComponent> ent, ref XenoResinWalkerActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Active &&
            !_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
        {
            return;
        }

        args.Handled = true;

        ent.Comp.Active = !ent.Comp.Active;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnXenoResinWalkerRefreshMovementSpeed(Entity<XenoResinWalkerComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Active &&
            TryComp(ent, out XenoComponent? xeno) &&
            xeno.OnWeeds)
        {
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
        }
    }

    private void OnXenoResinWalkerOnWeedsUpdated(Entity<XenoResinWalkerComponent> ent, ref XenoOnWeedsChangedEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }
}
