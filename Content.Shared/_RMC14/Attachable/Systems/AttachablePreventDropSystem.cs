using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Interaction.Components;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachablePreventDropSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachablePreventDropToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered);
    }

    private void OnAttachableAltered(Entity<AttachablePreventDropToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Activated:
                var comp = EnsureComp<UnremoveableComponent>(args.Holder);
                comp.DeleteOnDrop = false;
                Dirty(args.Holder, comp);
                break;

            case AttachableAlteredType.Deactivated:
                RemCompDeferred<UnremoveableComponent>(args.Holder);
                break;

            case AttachableAlteredType.DetachedDeactivated:
                RemCompDeferred<UnremoveableComponent>(args.Holder);
                break;
        }
    }
}
