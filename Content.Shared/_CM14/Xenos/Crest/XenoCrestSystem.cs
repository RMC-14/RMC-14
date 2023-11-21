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

    private void OnXenoCrestAction(Entity<XenoCrestComponent> ent, ref XenoToggleCrestActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Lowered = !ent.Comp.Lowered;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        var ev = new XenoCrestToggledEvent(ent.Comp.Lowered);
        foreach (var (id, _) in _actions.GetActions(ent))
        {
            RaiseLocalEvent(id, ref ev);
        }

        _appearance.SetData(ent, XenoVisualLayers.Crest, ent.Comp.Lowered);
    }

    private void OnXenoCrestRefreshMovementSpeed(Entity<XenoCrestComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Lowered)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnXenoCrestGetArmor(Entity<XenoCrestComponent> ent, ref XenoGetArmorEvent args)
    {
        if (ent.Comp.Lowered)
            args.Armor += ent.Comp.Armor;
    }

    private void OnXenoCrestBeforeStatusAdded(Entity<XenoCrestComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (ent.Comp.Lowered && args.Key == "Stun")
            args.Cancelled = true;
    }

    private void OnXenoCrestActionToggled(Entity<XenoCrestActionComponent> ent, ref XenoCrestToggledEvent args)
    {
        _actions.SetToggled(ent, args.Lowered);
    }
}
