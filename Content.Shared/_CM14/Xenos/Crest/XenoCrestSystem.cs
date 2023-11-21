using Content.Shared._CM14.Xenos.Armor;
using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;

namespace Content.Shared._CM14.Xenos.Crest;

public sealed class XenoCrestSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoCrestComponent, XenoToggleCrestActionEvent>(OnXenoCrestAction);
        SubscribeLocalEvent<XenoCrestComponent, RefreshMovementSpeedModifiersEvent>(OnXenoCrestRefreshMovementSpeed);
        SubscribeLocalEvent<XenoCrestComponent, XenoGetArmorEvent>(OnXenoCrestGetArmor);
        SubscribeLocalEvent<XenoCrestComponent, BeforeStatusEffectAddedEvent>(OnXenoCrestBeforeStatusAdded);

        SubscribeLocalEvent<XenoCrestActionComponent, XenoCrestToggledEvent>(OnXenoCrestActionToggled);
    }

    private void OnXenoCrestAction(Entity<XenoCrestComponent> xeno, ref XenoToggleCrestActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        xeno.Comp.Lowered = !xeno.Comp.Lowered;
        Dirty(xeno);

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

        var ev = new XenoCrestToggledEvent(xeno.Comp.Lowered);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }

        _appearance.SetData(xeno, XenoVisualLayers.Crest, xeno.Comp.Lowered);
    }

    private void OnXenoCrestRefreshMovementSpeed(Entity<XenoCrestComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (xeno.Comp.Lowered)
            args.ModifySpeed(xeno.Comp.SpeedMultiplier, xeno.Comp.SpeedMultiplier);
    }

    private void OnXenoCrestGetArmor(Entity<XenoCrestComponent> xeno, ref XenoGetArmorEvent args)
    {
        if (xeno.Comp.Lowered)
            args.Armor += xeno.Comp.Armor;
    }

    private void OnXenoCrestBeforeStatusAdded(Entity<XenoCrestComponent> xeno, ref BeforeStatusEffectAddedEvent args)
    {
        if (xeno.Comp.Lowered && args.Key == "Stun")
            args.Cancelled = true;
    }

    private void OnXenoCrestActionToggled(Entity<XenoCrestActionComponent> action, ref XenoCrestToggledEvent args)
    {
        _actions.SetToggled(action, args.Lowered);
    }
}
