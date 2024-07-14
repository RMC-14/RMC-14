using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Item;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    [Dependency] private readonly ItemSizeChangeSystem _itemSizeChangeSystem = default!;

    private void InitializeSize()
    {
        SubscribeLocalEvent<AttachableSizeModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableSizeModsComponent, GetItemSizeModifiersEvent>(OnGetItemSizeModifiers);
    }

    private void OnAttachableAltered(Entity<AttachableSizeModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (attachable.Comp.Modifiers.Count == 0)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _itemSizeChangeSystem.RefreshItemSizeModifiers(args.Holder);
                break;
        }
    }

    private void OnGetItemSizeModifiers(Entity<AttachableSizeModsComponent> attachable, ref GetItemSizeModifiersEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                return;

            args.Size += modSet.Size;
        }
    }
}
