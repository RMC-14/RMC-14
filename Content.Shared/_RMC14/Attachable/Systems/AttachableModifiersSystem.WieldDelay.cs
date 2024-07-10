using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Wieldable.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeWieldDelay()
    {
        SubscribeLocalEvent<AttachableWieldDelayModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableWieldDelayModsComponent, GetWieldDelayEvent>(OnGetWieldDelay);
    }

    private void OnAttachableAltered(Entity<AttachableWieldDelayModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            case AttachableAlteredType.Wielded:
                break;

            case AttachableAlteredType.Unwielded:
                break;

            default:
                _wieldableSystem.RefreshWieldDelay(args.Holder);
                break;
        }
    }

    private void OnGetWieldDelay(Entity<AttachableWieldDelayModsComponent> attachable, ref GetWieldDelayEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableWieldDelayModsComponent> attachable, AttachableWieldDelayModifierSet modSet, ref GetWieldDelayEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.Delay += modSet.Delay;
    }
}
