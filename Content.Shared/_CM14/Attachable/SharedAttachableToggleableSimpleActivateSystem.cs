using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared.Interaction;

namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableToggleableSimpleActivateSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableSimpleActivateComponent, AttachableAlteredEvent>(OnAttachableAltered);
    }

    private void OnAttachableAltered(Entity<AttachableToggleableSimpleActivateComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (args.User == null)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Activated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.Deactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.DetachedDeactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;
        }
    }
}
