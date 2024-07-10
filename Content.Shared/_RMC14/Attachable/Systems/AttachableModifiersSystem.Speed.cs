using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Wieldable;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeSpeed()
    {
        SubscribeLocalEvent<AttachableSpeedModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableSpeedModsComponent, GetWieldableSpeedModifiersEvent>(OnGetSpeedModifiers);
    }

    private void OnAttachableAltered(Entity<AttachableSpeedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
        }
    }

    private void OnGetSpeedModifiers(Entity<AttachableSpeedModsComponent> attachable, ref GetWieldableSpeedModifiersEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableSpeedModsComponent> attachable, AttachableSpeedModifierSet modSet, ref GetWieldableSpeedModifiersEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.Walk += modSet.Walk;
        args.Sprint += modSet.Sprint;
    }
}
