using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Wieldable;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable;

public sealed class AttachableSpeedModsSystem : EntitySystem
{
    [Dependency] private readonly RMCWieldableSystem _wieldableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableSpeedModsWieldableComponent, AttachableAlteredEvent>(OnWieldableAttachableAltered);
        SubscribeLocalEvent<AttachableSpeedModsWieldableComponent, GetWieldableSpeedModifiersEvent>(OnWieldableGetSpeedModifiers);

        SubscribeLocalEvent<AttachableSpeedModsToggleableComponent, AttachableAlteredEvent>(OnToggleableAttachableAltered);
        SubscribeLocalEvent<AttachableSpeedModsToggleableComponent, GetWieldableSpeedModifiersEvent>(OnToggleableGetSpeedModifiers);
    }

    private void OnWieldableAttachableAltered(Entity<AttachableSpeedModsWieldableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
            
            case AttachableAlteredType.Detached:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
        }
    }

    private void OnWieldableGetSpeedModifiers(Entity<AttachableSpeedModsWieldableComponent> attachable, ref GetWieldableSpeedModifiersEvent args)
    {
        args.UnwieldedWalk += attachable.Comp.Unwielded.Walk;
        args.UnwieldedSprint += attachable.Comp.Unwielded.Sprint;
        args.WieldedWalk += attachable.Comp.Wielded.Walk;
        args.WieldedSprint += attachable.Comp.Wielded.Sprint;
    }

    private void OnToggleableAttachableAltered(Entity<AttachableSpeedModsToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
            
            case AttachableAlteredType.Detached:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
            
            case AttachableAlteredType.Activated:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
            
            case AttachableAlteredType.Deactivated:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
        }
    }

    private void OnToggleableGetSpeedModifiers(Entity<AttachableSpeedModsToggleableComponent> attachable, ref GetWieldableSpeedModifiersEvent args)
    {
        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent) || !toggleableComponent.Active)
        {
            args.UnwieldedWalk += attachable.Comp.InactiveUnwielded.Walk;
            args.UnwieldedSprint += attachable.Comp.InactiveUnwielded.Sprint;
            args.WieldedWalk += attachable.Comp.InactiveWielded.Walk;
            args.WieldedSprint += attachable.Comp.InactiveWielded.Sprint;
            return;
        }
        
        args.UnwieldedWalk += attachable.Comp.ActiveUnwielded.Walk;
        args.UnwieldedSprint += attachable.Comp.ActiveUnwielded.Sprint;
        args.WieldedWalk += attachable.Comp.ActiveWielded.Walk;
        args.WieldedSprint += attachable.Comp.ActiveWielded.Sprint;
    }
}
